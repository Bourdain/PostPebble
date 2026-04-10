using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Api.LinkedIn;
using Api.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Scheduler;

public sealed class SchedulerPublishWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SchedulerWorkerOptions> optionsAccessor,
    ILogger<SchedulerPublishWorker> logger) : BackgroundService
{
    private readonly SchedulerWorkerOptions _options = optionsAccessor.Value;

    private static readonly int[] BackoffSeconds = [60, 300, 1800]; // 1m, 5m, 30m

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduler publish worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDuePostsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduler publish worker loop failed.");
            }

            var delaySeconds = _options.PollIntervalSeconds <= 0 ? 20 : _options.PollIntervalSeconds;
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task ProcessDuePostsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var schedulerDbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var integrationsDbContext = scope.ServiceProvider.GetRequiredService<IntegrationsDbContext>();
        var mediaDbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var reservationLedgerService = scope.ServiceProvider.GetRequiredService<IReservationLedgerService>();
        var linkedInPublisher = scope.ServiceProvider.GetRequiredService<LinkedInPublisher>();
        var mediaStorage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();

        var now = DateTime.UtcNow;
        var batchSize = _options.BatchSize <= 0 ? 20 : _options.BatchSize;

        // Pick up posts that are Queued and due, or Queued and past their retry time
        var duePosts = await schedulerDbContext.ScheduledPosts
            .Where(x =>
                x.Status == ScheduledPostStatus.Queued
                && x.ScheduledAtUtc <= now
                && (x.NextRetryAtUtc == null || x.NextRetryAtUtc <= now))
            .Include(x => x.Targets)
            .Include(x => x.MediaLinks)
            .OrderBy(x => x.ScheduledAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var post in duePosts)
        {
            post.Status = ScheduledPostStatus.Publishing;
            await schedulerDbContext.SaveChangesAsync(cancellationToken);

            var successful = true;
            string? failureReason = null;

            foreach (var target in post.Targets)
            {
                if (target.Platform != SocialPlatform.LinkedIn)
                {
                    successful = false;
                    failureReason = $"Platform {target.Platform} publishing not implemented yet in worker.";
                    break;
                }

                try
                {
                    var connection = await integrationsDbContext.LinkedInConnections
                        .SingleOrDefaultAsync(x => x.TenantId == post.TenantId, cancellationToken);
                    if (connection is null)
                    {
                        successful = false;
                        failureReason = "No LinkedIn connection found for tenant.";
                        break;
                    }

                    // Check if post has attached media
                    if (post.MediaLinks.Count > 0)
                    {
                        var mediaIds = post.MediaLinks.Select(m => m.MediaAssetId).ToList();
                        var mediaAssets = await mediaDbContext.MediaAssets
                            .Where(m => mediaIds.Contains(m.Id))
                            .ToListAsync(cancellationToken);

                        // Only upload image media; skip non-image types for now
                        var imageAssets = mediaAssets
                            .Where(m => m.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (imageAssets.Count > 0)
                        {
                            var payloads = new List<MediaPayload>();
                            try
                            {
                                foreach (var asset in imageAssets)
                                {
                                    var stream = await mediaStorage.GetFileStreamAsync(post.TenantId, asset.StoredFileName, cancellationToken);
                                    if (stream is null)
                                    {
                                        logger.LogWarning("Media file not found on disk: {StoredFileName} for post {PostId}", asset.StoredFileName, post.Id);
                                        continue;
                                    }

                                    payloads.Add(new MediaPayload
                                    {
                                        Content = stream,
                                        ContentType = asset.ContentType,
                                        FileName = asset.OriginalFileName
                                    });
                                }

                                await linkedInPublisher.PublishWithMediaAsync(connection, post, payloads, cancellationToken);
                            }
                            finally
                            {
                                foreach (var payload in payloads)
                                {
                                    payload.Dispose();
                                }
                            }
                        }
                        else
                        {
                            // No image media, publish text only
                            await linkedInPublisher.PublishTextAsync(connection, post, cancellationToken);
                        }
                    }
                    else
                    {
                        await linkedInPublisher.PublishTextAsync(connection, post, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    successful = false;
                    failureReason = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    break;
                }
            }

            if (successful)
            {
                await reservationLedgerService.ConsumeReservationAsync(post.ReservationId, $"worker_success:{post.Id}", cancellationToken);
                post.Status = ScheduledPostStatus.Published;
                post.SettledAtUtc = DateTime.UtcNow;
                post.FailureReason = null;
            }
            else
            {
                // Retry logic: if we haven't exhausted retries, schedule next attempt
                if (post.RetryCount < post.MaxRetries)
                {
                    post.RetryCount++;
                    var backoffIndex = Math.Min(post.RetryCount - 1, BackoffSeconds.Length - 1);
                    post.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(BackoffSeconds[backoffIndex]);
                    post.Status = ScheduledPostStatus.Queued; // Put back in queue
                    post.FailureReason = $"Attempt {post.RetryCount}/{post.MaxRetries} failed: {failureReason}";
                    logger.LogWarning(
                        "Post {PostId} publish attempt {Attempt}/{Max} failed. Next retry at {NextRetry}. Reason: {Reason}",
                        post.Id, post.RetryCount, post.MaxRetries, post.NextRetryAtUtc, failureReason);
                }
                else
                {
                    // All retries exhausted — refund credits
                    await reservationLedgerService.ReleaseReservationAsync(post.ReservationId, $"worker_failed:{post.Id}", cancellationToken);
                    post.Status = ScheduledPostStatus.Refunded;
                    post.SettledAtUtc = DateTime.UtcNow;
                    post.FailureReason = failureReason ?? "Worker failed to publish after all retries.";
                    logger.LogError("Post {PostId} permanently failed after {Max} retries. Credits refunded.", post.Id, post.MaxRetries);
                }
            }

            await schedulerDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
