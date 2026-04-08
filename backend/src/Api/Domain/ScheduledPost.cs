namespace Api.Domain;

public sealed class ScheduledPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid ReservationId { get; set; }
    public ScheduledPostStatus Status { get; set; } = ScheduledPostStatus.Queued;
    public string? FailureReason { get; set; }
    public DateTime? SettledAtUtc { get; set; }

    public Tenant? Tenant { get; set; }
    public User? CreatedByUser { get; set; }
    public CreditReservation? Reservation { get; set; }
    public List<PostTarget> Targets { get; set; } = [];
    public List<ScheduledPostMedia> MediaLinks { get; set; } = [];
}
