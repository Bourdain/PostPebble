namespace Api.Domain;

public enum ScheduledPostStatus
{
    Queued = 0,
    Publishing = 1,
    Published = 2,
    Failed = 3,
    Refunded = 4
}
