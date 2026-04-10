using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Scheduler;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics").RequireAuthorization();
        group.MapGet("/{tenantId:guid}/summary", GetAnalyticsSummaryAsync);
        return app;
    }

    private static async Task<IResult> GetAnalyticsSummaryAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        SchedulerDbContext schedulerDbContext,
        BillingDbContext billingDbContext,
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

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        // Fetch all posts within 30 days that have a settled status
        var recentPosts = await schedulerDbContext.ScheduledPosts
            .Where(x => x.TenantId == tenantId && x.CreatedAtUtc >= thirtyDaysAgo)
            .Include(x => x.Targets)
            .ToListAsync(cancellationToken);

        var allPosts = await schedulerDbContext.ScheduledPosts
            .Where(x => x.TenantId == tenantId)
            .CountAsync(cancellationToken);

        // Published counts
        var publishedLast7 = recentPosts.Count(p => p.Status == ScheduledPostStatus.Published && p.SettledAtUtc >= sevenDaysAgo);
        var publishedLast30 = recentPosts.Count(p => p.Status == ScheduledPostStatus.Published);
        var failedLast30 = recentPosts.Count(p => p.Status is ScheduledPostStatus.Refunded or ScheduledPostStatus.Failed);
        var queuedCount = recentPosts.Count(p => p.Status == ScheduledPostStatus.Queued);

        // Posts per day (last 30 days, grouped by date)
        var postsPerDay = recentPosts
            .Where(p => p.Status == ScheduledPostStatus.Published && p.SettledAtUtc.HasValue)
            .GroupBy(p => p.SettledAtUtc!.Value.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        // Platform breakdown (count of targets across published posts)
        var platformBreakdown = recentPosts
            .Where(p => p.Status == ScheduledPostStatus.Published)
            .SelectMany(p => p.Targets)
            .GroupBy(t => t.Platform.ToString())
            .Select(g => new { platform = g.Key, count = g.Count() })
            .ToList();

        // Credit usage (last 30 days)
        var creditTransactions = await billingDbContext.CreditTransactions
            .Where(x => x.TenantId == tenantId && x.CreatedAtUtc >= thirtyDaysAgo)
            .ToListAsync(cancellationToken);

        var creditsPurchased = creditTransactions
            .Where(t => t.Type == CreditTransactionType.Purchase)
            .Sum(t => t.AmountCredits);

        var creditsConsumed = creditTransactions
            .Where(t => t.Type == CreditTransactionType.Consume || t.Type == CreditTransactionType.Reserve)
            .Sum(t => Math.Abs(t.AmountCredits));

        // Success rate
        var totalSettled = publishedLast30 + failedLast30;
        var successRate = totalSettled > 0 ? Math.Round((double)publishedLast30 / totalSettled * 100, 1) : 100.0;

        return Results.Ok(new
        {
            totalPosts = allPosts,
            publishedLast7Days = publishedLast7,
            publishedLast30Days = publishedLast30,
            failedLast30Days = failedLast30,
            queuedCount,
            successRate,
            creditsPurchasedLast30Days = creditsPurchased,
            creditsConsumedLast30Days = creditsConsumed,
            postsPerDay,
            platformBreakdown
        });
    }
}
