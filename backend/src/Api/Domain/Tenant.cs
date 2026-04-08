namespace Api.Domain;

public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Membership> Memberships { get; set; } = [];
}
