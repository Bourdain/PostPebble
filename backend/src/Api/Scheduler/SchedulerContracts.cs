using Api.Domain;

namespace Api.Scheduler;

public sealed record PostTargetRequest(SocialPlatform Platform, string ExternalAccountId);
public sealed record CreateScheduledPostRequest(
    Guid TenantId,
    string TextContent,
    DateTime ScheduledAtUtc,
    List<PostTargetRequest> Targets,
    List<Guid>? MediaAssetIds = null
);
