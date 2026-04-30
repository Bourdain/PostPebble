using Api.Domain;
using Api.Infrastructure;

namespace Api.Notifications;

public sealed class NotificationService(AppDbContext dbContext)
{
    public async Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string body,
        Guid? tenantId = null,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(new AppNotification
        {
            UserId = userId,
            TenantId = tenantId,
            Type = type,
            Title = title,
            Body = body,
            LinkUrl = linkUrl
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
