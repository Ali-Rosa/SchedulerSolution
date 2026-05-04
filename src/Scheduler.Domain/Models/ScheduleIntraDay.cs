namespace Scheduler.Domain.Models;

public sealed record ScheduleIntraDay(
    bool OcursOnceEnable,
    TimeOnly OnceTime,
    bool OcursEveryEnable,
    IntraDayFrequencyUnit Unit,
    int Every,
    TimeOnly StartTime,
    TimeOnly EndTime
);
