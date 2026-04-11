using Api.Domain;

namespace Api.Scheduler;

public sealed record PostTargetRequest(SocialPlatform Platform, string ExternalAccountId);
public sealed record CreateScheduledPostRequest(
    Guid TenantId,
    string TextContent,
    DateTime ScheduledAtUtc,
    List<PostTargetRequest> Targets,
    List<Guid>? MediaAssetIds = null,
    bool QueueImmediately = false
);

public sealed record UpdateScheduledPostRequest(
    string? TextContent = null,
    DateTime? ScheduledAtUtc = null,
    List<PostTargetRequest>? Targets = null,
    List<Guid>? MediaAssetIds = null
);
