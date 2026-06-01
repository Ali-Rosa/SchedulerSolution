using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Models;

public record SchedulerConfiguration
{
    public DateTimeOffset CurrentDate { get; init; }
    public bool Enabled { get; init; }
    public SchedulerType Type { get; init; }
    public OccursType Occurs { get; init; }
    public DateTimeOffset? ExecutionDateTimeLocal { get; init; }
    public int RecursEvery { get; init; }
    public DateTimeOffset? LimitsStartDateLocal { get; init; }
    public DateTimeOffset? LimitsEndDateLocal { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public DayOfWeek? FirstDayOfWeek { get; init; } = null;
    public int MaxOccurrences { get; init; } = 1;
    public ScheduleDailyFrequency? DailyFrequencyConfiguration { get; init; } = null;
    public SchedulerWeekly? WeeklyConfiguration { get; init; } = null;
    public SchedulerMonthly? MonthlyConfiguration { get; init; } = null;
}