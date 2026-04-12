namespace Api.Domain;

public sealed class Membership
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Drafter;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Tenant? Tenant { get; set; }
}
