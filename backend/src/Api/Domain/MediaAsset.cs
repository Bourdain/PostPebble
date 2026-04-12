namespace Api.Domain;

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = [];

    public Tenant? Tenant { get; set; }
    public User? UploadedByUser { get; set; }
    public List<ScheduledPostMedia> ScheduledPostLinks { get; set; } = [];
}
