namespace Api.Scheduler;

public sealed class SchedulerWorkerOptions
{
    public const string SectionName = "SchedulerWorker";
    public int PollIntervalSeconds { get; set; } = 20;
    public int BatchSize { get; set; } = 20;
}
