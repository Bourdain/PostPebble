namespace Api.Domain;

public sealed class CreditReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public int AmountCredits { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public CreditReservationStatus Status { get; set; } = CreditReservationStatus.Reserved;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAtUtc { get; set; }

    public Tenant? Tenant { get; set; }
}
