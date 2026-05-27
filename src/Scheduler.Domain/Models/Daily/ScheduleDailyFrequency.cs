namespace Scheduler.Domain.Models.Daily;

public sealed record ScheduleDailyFrequency
{
    public TimeOnly OnceTime { get; init; }
    public bool OccursEveryEnable { get; init; }
    public TimeIntervalUnit IntervalUnit { get; init; }
    public int FrequencyInterval { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}