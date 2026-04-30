namespace Api.Domain;

public enum ScheduledPostStatus
{
    PendingApproval = -2,
    Draft = -1,
    Queued = 0,
    Publishing = 1,
    Published = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}
