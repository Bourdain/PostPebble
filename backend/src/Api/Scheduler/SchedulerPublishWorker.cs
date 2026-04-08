using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Api.LinkedIn;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Scheduler;

public sealed class SchedulerPublishWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SchedulerWorkerOptions> optionsAccessor,
    ILogger<SchedulerPublishWorker> logger) : BackgroundService
{
    private readonly SchedulerWorkerOptions _options = optionsAccessor.Value;

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
        var reservationLedgerService = scope.ServiceProvider.GetRequiredService<IReservationLedgerService>();
        var linkedInPublisher = scope.ServiceProvider.GetRequiredService<LinkedInPublisher>();

        var now = DateTime.UtcNow;
        var batchSize = _options.BatchSize <= 0 ? 20 : _options.BatchSize;
        var duePosts = await schedulerDbContext.ScheduledPosts
            .Where(x => x.Status == ScheduledPostStatus.Queued && x.ScheduledAtUtc <= now)
            .Include(x => x.Targets)
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

                    await linkedInPublisher.PublishTextAsync(connection, post, cancellationToken);
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
                await reservationLedgerService.ReleaseReservationAsync(post.ReservationId, $"worker_failed:{post.Id}", cancellationToken);
                post.Status = ScheduledPostStatus.Refunded;
                post.SettledAtUtc = DateTime.UtcNow;
                post.FailureReason = failureReason ?? "Worker failed to publish.";
            }

            await schedulerDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
