namespace Api.Domain;

public sealed class TenantInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public TenantRole Role { get; set; } = TenantRole.Drafter;
    public string Code { get; set; } = string.Empty;
    public TenantInviteStatus Status { get; set; } = TenantInviteStatus.Pending;
    public Guid InvitedByUserId { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }

    public Tenant? Tenant { get; set; }
    public User? InvitedByUser { get; set; }
    public User? AcceptedByUser { get; set; }
}
