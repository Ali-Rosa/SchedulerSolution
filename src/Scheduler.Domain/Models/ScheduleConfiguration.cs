namespace Scheduler.Domain.Models;

public record ScheduleConfiguration(
    bool Enabled,
    ScheduleType Type,
    DateTimeOffset? ExecutionDateTimeLocal,
    OccursType Occurs,
    int RecursEvery,
    DateTimeOffset? LimitsStartDateLocal,
    DateTimeOffset? LimitsEndDateLocal,
    string TimeZoneId,
    ScheduleDailyFrecuency? DailyFrecuency,
    ScheduleWeekly? Weekly
);