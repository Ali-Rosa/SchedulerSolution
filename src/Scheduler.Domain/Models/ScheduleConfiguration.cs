namespace Scheduler.Domain.Models;

public record ScheduleConfiguration(
    bool Enabled,
    ScheduleType Type,
    DateTimeOffset? ExecutionDateTimeLocal,
    OccursType Occurs,
    int Every,
    DateTimeOffset? StartDateLocal,
    DateTimeOffset? EndDateLocal,
    string TimeZoneId,
    IntraDaySchedule? IntraDay,
    WeeklySchedule? Weekly

);