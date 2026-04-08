namespace Api.Domain;

public sealed class LinkedInOAuthState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string State { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAtUtc { get; set; }
}
