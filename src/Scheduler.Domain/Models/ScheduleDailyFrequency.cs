namespace Scheduler.Domain.Models;

public sealed record ScheduleDailyFrequency (
    bool OccursOnceEnable,
    TimeOnly OnceTime,
    bool OccursEveryEnable,
    SchedulerTimeIntervalUnit IntervalUnit,
    int FrequencyInterval,
    TimeOnly StartTime,
    TimeOnly EndTime
);