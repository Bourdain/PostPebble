using System.Security.Claims;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization();
        group.MapGet("/summary", GetSummaryAsync);
        group.MapGet("/recent", GetRecentAsync);
        group.MapGet("/", GetPageAsync);
        group.MapPost("/{notificationId:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
        return app;
    }

    private static async Task<IResult> GetSummaryAsync(
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var unreadCount = await dbContext.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        return Results.Ok(new { unreadCount });
    }

    private static async Task<IResult> GetRecentAsync(
        int take,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        take = take <= 0 ? 5 : Math.Min(take, 20);

        var items = await dbContext.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationListItem(
                x.Id,
                x.Type,
                x.Title,
                x.Body,
                x.LinkUrl,
                x.TenantId,
                x.Tenant != null ? x.Tenant.Name : null,
                x.IsRead,
                x.CreatedAtUtc))
            .Take(take)
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetPageAsync(
        int page,
        int pageSize,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);

        var query = dbContext.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationListItem(
                x.Id,
                x.Type,
                x.Title,
                x.Body,
                x.LinkUrl,
                x.TenantId,
                x.Tenant != null ? x.Tenant.Name : null,
                x.IsRead,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(new NotificationPageResponse(items, totalCount, page, pageSize));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notificationId,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return Results.NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new { message = "Notification marked as read." });
    }

    private static async Task<IResult> MarkAllReadAsync(
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var notifications = await dbContext.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "All notifications marked as read." });
    }
}

public sealed record NotificationListItem(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? LinkUrl,
    Guid? TenantId,
    string? TenantName,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record NotificationPageResponse(
    IReadOnlyList<NotificationListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
