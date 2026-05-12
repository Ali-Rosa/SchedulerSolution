namespace Scheduler.Domain.Models;

public record ScheduleConfiguration (
    bool Enabled,
    ScheduleType Type,
    DateTimeOffset? ExecutionDateTimeLocal,
    OccursType Occurs,
    int RecursEvery,
    DateTimeOffset? LimitsStartDateLocal,
    DateTimeOffset? LimitsEndDateLocal,
    string TimeZoneId,
    string? Locale = null,
    DayOfWeek? FirstDayOfWeek = null,
    ScheduleDailyFrecuency? DailyFrecuency = null,
    ScheduleWeekly? Weekly = null
);