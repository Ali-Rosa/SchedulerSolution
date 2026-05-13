namespace Scheduler.Domain.Models;

public sealed record ScheduleDailyFrequency (
    bool OccursOnceEnable,
    TimeOnly OnceTime,
    bool OccursEveryEnable,
    TimeIntervalUnit IntervalUnit,
    int FrequencyInterval,
    TimeOnly StartTime,
    TimeOnly EndTime
);