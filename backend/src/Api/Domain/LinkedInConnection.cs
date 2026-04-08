namespace Api.Domain;

public sealed class LinkedInConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ConnectedByUserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? MemberUrn { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
