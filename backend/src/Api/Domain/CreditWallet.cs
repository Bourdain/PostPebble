namespace Api.Domain;

public sealed class CreditWallet
{
    public Guid TenantId { get; set; }
    public int BalanceCredits { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
