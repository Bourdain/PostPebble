using System.Security.Cryptography;
using System.Text;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Billing;

public sealed class StripeWebhookService(BillingDbContext dbContext)
{
    public async Task<bool> TryBeginEventAsync(string eventId, string eventType, string rawPayload, Guid? tenantId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.StripeWebhookEvents.AnyAsync(x => x.Id == eventId, cancellationToken);
        if (exists)
        {
            return false;
        }

        dbContext.StripeWebhookEvents.Add(new StripeWebhookEvent
        {
            Id = eventId,
            EventType = eventType,
            PayloadHash = Sha256(rawPayload),
            TenantId = tenantId,
            Status = "Received",
            ReceivedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkProcessedAsync(string eventId, CancellationToken cancellationToken)
    {
        var row = await dbContext.StripeWebhookEvents.SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.ProcessedAtUtc = DateTime.UtcNow;
        row.Status = "Processed";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(string eventId, string errorMessage, CancellationToken cancellationToken)
    {
        var row = await dbContext.StripeWebhookEvents.SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.Status = "Failed";
        row.ErrorMessage = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
