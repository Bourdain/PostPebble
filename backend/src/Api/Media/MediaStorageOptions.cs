namespace Api.Media;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "video/mp4",
        "video/quicktime"
    ];
}
