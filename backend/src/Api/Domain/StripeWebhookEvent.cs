namespace Api.Domain;

public sealed class StripeWebhookEvent
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string Status { get; set; } = "Received";
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
}
