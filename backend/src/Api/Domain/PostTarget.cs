namespace Api.Domain;

public sealed class PostTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduledPostId { get; set; }
    public SocialPlatform Platform { get; set; }
    public string ExternalAccountId { get; set; } = string.Empty;

    public ScheduledPost? ScheduledPost { get; set; }
}
