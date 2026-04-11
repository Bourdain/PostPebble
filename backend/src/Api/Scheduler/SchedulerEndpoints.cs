using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Scheduler;

public static class SchedulerEndpoints
{
    public static IEndpointRouteBuilder MapSchedulerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scheduler").RequireAuthorization();
        group.MapPost("/posts", CreateScheduledPostAsync);
        group.MapGet("/posts/{tenantId:guid}", ListScheduledPostsAsync);
        group.MapPut("/posts/{postId:guid}", UpdateScheduledPostAsync);
        group.MapPost("/posts/{postId:guid}/mark-publishing", MarkPublishingAsync);
        group.MapPost("/posts/{postId:guid}/mark-success", MarkSuccessAsync);
        group.MapPost("/posts/{postId:guid}/mark-failed", MarkFailedAsync);
        group.MapPost("/tenants/{tenantId:guid}/reconcile", ReconcileReservationsAsync);
        group.MapPost("/posts/{postId:guid}/mark-cancelled", MarkCancelledAsync);
        return app;
    }

    private static async Task<IResult> MarkCancelledAsync(
        Guid postId,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        ITenantAccessService tenantAccessService,
        IReservationLedgerService reservationLedgerService,
        CancellationToken cancellationToken)
    {
        
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await dbContext.ScheduledPosts.SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, post.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        if (post.ReservationId.HasValue)
        {
            var released = await reservationLedgerService.ReleaseReservationAsync(post.ReservationId.Value, $"publish_failed:{postId}", cancellationToken);
            if (!released)
            {
                return Results.Conflict("Reservation already settled or missing.");
            }
        }

        post.Status = ScheduledPostStatus.Cancelled;
        post.SettledAtUtc = DateTime.UtcNow;
        post.FailureReason = $"Publish cancelled by user ({userId}).";
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { postId, status = "cancelled", creditsReleased = true });
    }


    private static async Task<IResult> CreateScheduledPostAsync(
        CreateScheduledPostRequest request,
        ClaimsPrincipal principal,
        SchedulerDbContext schedulerDbContext,
        MediaDbContext mediaDbContext,
        ITenantAccessService tenantAccessService,
        IReservationLedgerService reservationLedgerService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.Targets is null || request.Targets.Count == 0)
        {
            return Results.BadRequest("At least one target is required.");
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, request.TenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var creditsNeeded = request.Targets.Count;
        var reservation = await reservationLedgerService.ReserveAsync(
            request.TenantId,
            creditsNeeded,
            $"schedule:{Guid.NewGuid()}",
            $"Reserved for scheduled cross-post ({creditsNeeded} targets).",
            cancellationToken
        );
        if (reservation is null)
        {
            return Results.BadRequest(new { error = "Insufficient credits." });
        }

        var post = new ScheduledPost
        {
            TenantId = request.TenantId,
            CreatedByUserId = userId.Value,
            TextContent = request.TextContent.Trim(),
            ScheduledAtUtc = request.ScheduledAtUtc,
            ReservationId = reservation.Id,
            Status = ScheduledPostStatus.Queued
        };
        post.Targets = request.Targets
            .Select(x => new PostTarget
            {
                Platform = x.Platform,
                ExternalAccountId = x.ExternalAccountId.Trim()
            })
            .ToList();

        if (request.MediaAssetIds is { Count: > 0 })
        {
            var mediaAssets = await mediaDbContext.MediaAssets
                .Where(x => x.TenantId == request.TenantId && request.MediaAssetIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            if (mediaAssets.Count != request.MediaAssetIds.Count)
            {
                return Results.BadRequest("One or more media assets are invalid for this tenant.");
            }

            post.MediaLinks = request.MediaAssetIds
                .Distinct()
                .Select(mediaId => new ScheduledPostMedia
                {
                    MediaAssetId = mediaId
                })
                .ToList();
        }

        schedulerDbContext.ScheduledPosts.Add(post);
        await schedulerDbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            post.Id,
            reservedCredits = creditsNeeded,
            reservationId = reservation.Id,
            post.ScheduledAtUtc
        });
    }

    private static async Task<IResult> UpdateScheduledPostAsync(
        Guid postId,
        UpdateScheduledPostRequest request,
        ClaimsPrincipal principal,
        SchedulerDbContext schedulerDbContext,
        MediaDbContext mediaDbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await schedulerDbContext.ScheduledPosts
            .Include(x => x.Targets)
            .Include(x => x.MediaLinks)
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, post.TenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        if (post.Status != ScheduledPostStatus.Queued && post.Status != ScheduledPostStatus.Draft)
        {
            return Results.Conflict("Only queued or draft posts can be edited.");
        }

        if (request.TextContent is not null)
        {
            post.TextContent = request.TextContent.Trim();
        }

        if (request.ScheduledAtUtc.HasValue)
        {
            post.ScheduledAtUtc = request.ScheduledAtUtc.Value;
        }

        if (request.Targets is { Count: > 0 })
        {
            schedulerDbContext.PostTargets.RemoveRange(post.Targets);
            post.Targets = request.Targets
                .Select(x => new PostTarget
                {
                    Platform = x.Platform,
                    ExternalAccountId = x.ExternalAccountId.Trim()
                })
                .ToList();
        }

        if (request.MediaAssetIds is not null)
        {
            schedulerDbContext.ScheduledPostMediaLinks.RemoveRange(post.MediaLinks);

            if (request.MediaAssetIds.Count > 0)
            {
                var validMedia = await mediaDbContext.MediaAssets
                    .Where(x => x.TenantId == post.TenantId && request.MediaAssetIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                post.MediaLinks = validMedia
                    .Select(id => new ScheduledPostMedia { MediaAssetId = id })
                    .ToList();
            }
            else
            {
                post.MediaLinks = [];
            }
        }

        await schedulerDbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            post.Id,
            post.TextContent,
            post.ScheduledAtUtc,
            targets = post.Targets.Select(t => new { platform = t.Platform.ToString(), t.ExternalAccountId }),
            mediaCount = post.MediaLinks.Count
        });
    }

    private static async Task<IResult> ListScheduledPostsAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        MediaDbContext mediaDbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, tenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var posts = await dbContext.ScheduledPosts
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.Targets)
            .Include(x => x.MediaLinks)
            .OrderByDescending(x => x.ScheduledAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var mediaIds = posts
            .SelectMany(x => x.MediaLinks)
            .Select(x => x.MediaAssetId)
            .Distinct()
            .ToList();

        var mediaById = await mediaDbContext.MediaAssets
            .Where(x => mediaIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => new { x.OriginalFileName, x.PublicUrl, x.ContentType },
                cancellationToken
            );

        var rows = posts.Select(x => new
        {
            x.Id,
            x.TextContent,
            x.ScheduledAtUtc,
            x.CreatedAtUtc,
            x.ReservationId,
            status = x.Status.ToString(),
            x.FailureReason,
            x.SettledAtUtc,
            x.RetryCount,
            x.MaxRetries,
            x.NextRetryAtUtc,
            targets = x.Targets.Select(t => new { platform = t.Platform.ToString(), t.ExternalAccountId }),
            media = x.MediaLinks
                .Select(m => mediaById.TryGetValue(m.MediaAssetId, out var media) ? new
                {
                    m.MediaAssetId,
                    fileName = media.OriginalFileName,
                    media.PublicUrl,
                    media.ContentType
                } : null)
                .Where(m => m is not null)
        });

        return Results.Ok(rows);
    }

    private static async Task<IResult> MarkPublishingAsync(
        Guid postId,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await dbContext.ScheduledPosts.SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, post.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        if (post.Status != ScheduledPostStatus.Queued)
        {
            return Results.Conflict("Post is not in queued state.");
        }

        post.Status = ScheduledPostStatus.Publishing;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { postId, status = post.Status.ToString() });
    }

    private static async Task<IResult> MarkSuccessAsync(
        Guid postId,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        ITenantAccessService tenantAccessService,
        IReservationLedgerService reservationLedgerService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await dbContext.ScheduledPosts.SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, post.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        if (post.ReservationId.HasValue)
        {
            var settled = await reservationLedgerService.ConsumeReservationAsync(post.ReservationId.Value, $"publish_success:{postId}", cancellationToken);
            if (!settled)
            {
                return Results.Conflict("Reservation already settled or missing.");
            }
        }

        post.Status = ScheduledPostStatus.Published;
        post.SettledAtUtc = DateTime.UtcNow;
        post.FailureReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { postId, status = "success", creditsSettled = true });
    }

    private static async Task<IResult> MarkFailedAsync(
        Guid postId,
        MarkFailedRequest request,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        ITenantAccessService tenantAccessService,
        IReservationLedgerService reservationLedgerService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await dbContext.ScheduledPosts.SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, post.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        if (post.ReservationId.HasValue)
        {
            var released = await reservationLedgerService.ReleaseReservationAsync(post.ReservationId.Value, $"publish_failed:{postId}", cancellationToken);
            if (!released)
            {
                return Results.Conflict("Reservation already settled or missing.");
            }
        }

        post.Status = ScheduledPostStatus.Refunded;
        post.SettledAtUtc = DateTime.UtcNow;
        post.FailureReason = string.IsNullOrWhiteSpace(request.Reason) ? "Publish failed." : request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { postId, status = "failed", creditsReleased = true });
    }

    private static async Task<IResult> ReconcileReservationsAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        SchedulerDbContext dbContext,
        ITenantAccessService tenantAccessService,
        IReservationLedgerService reservationLedgerService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, tenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        var stalePosts = await dbContext.ScheduledPosts
            .Where(x => x.TenantId == tenantId
                && x.Status == ScheduledPostStatus.Publishing
                && x.ScheduledAtUtc < DateTime.UtcNow.AddHours(-2))
            .ToListAsync(cancellationToken);

        var releasedCount = 0;
        foreach (var post in stalePosts)
        {
            if (post.ReservationId.HasValue)
            {
                var released = await reservationLedgerService.ReleaseReservationAsync(
                    post.ReservationId.Value,
                    $"reconcile:{post.Id}",
                    cancellationToken
                );
                if (!released)
                {
                    continue;
                }
            }

            post.Status = ScheduledPostStatus.Refunded;
            post.SettledAtUtc = DateTime.UtcNow;
            post.FailureReason = "Auto-refunded by reconciliation job.";
            releasedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { tenantId, stalePublishingPosts = stalePosts.Count, refunded = releasedCount });
    }
}

public sealed record MarkFailedRequest(string? Reason);
