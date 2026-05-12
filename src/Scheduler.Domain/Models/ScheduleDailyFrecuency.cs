namespace Scheduler.Domain.Models;

public sealed record ScheduleDailyFrecuency (
    bool OccursOnceEnable,
    TimeOnly OnceTime,
    bool OccursEveryEnable,
    TimeIntervalUnit IntervalUnit,
    int FrequencyInterval,
    TimeOnly StartTime,
    TimeOnly EndTime
);