namespace Scheduler.Domain.Models;

public record SchedulerConfiguration (
    bool Enabled,
    SchedulerType Type,
    DateTimeOffset? ExecutionDateTimeLocal,
    SchedulerOccursType Occurs,
    int RecursEvery,
    DateTimeOffset? LimitsStartDateLocal,
    DateTimeOffset? LimitsEndDateLocal,
    string TimeZoneId,
    string? Locale = null,
    DayOfWeek? FirstDayOfWeek = null,
    ScheduleDailyFrequency? DailyFrequency = null,
    SchedulerWeekly? Weekly = null,
    SchedulerMonthly? Monthly = null
);