namespace Api.Domain;

public sealed class ScheduledPostMedia
{
    public Guid ScheduledPostId { get; set; }
    public Guid MediaAssetId { get; set; }

    public ScheduledPost? ScheduledPost { get; set; }
    public MediaAsset? MediaAsset { get; set; }
}
