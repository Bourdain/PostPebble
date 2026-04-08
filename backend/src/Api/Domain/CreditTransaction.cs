namespace Api.Domain;

public sealed class CreditTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public CreditTransactionType Type { get; set; }
    public int AmountCredits { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
