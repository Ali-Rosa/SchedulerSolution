using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Models;

public record SchedulerConfiguration
{
    public bool Enabled { get; init; }
    public SchedulerType Type { get; init; }
    public DateTimeOffset? ExecutionDateTimeLocal { get; init; }
    public OccursType Occurs { get; init; }
    public int RecursEvery { get; init; }
    public DateTimeOffset? LimitsStartDateLocal { get; init; }
    public DateTimeOffset? LimitsEndDateLocal { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public DayOfWeek? FirstDayOfWeek { get; init; } = null;
    public ScheduleDailyFrequency? DailyFrequencyConfiguration { get; init; } = null;
    public SchedulerWeekly? WeeklyConfiguration { get; init; } = null;
    public SchedulerMonthly? MonthlyConfiguration { get; init; } = null;

    public (bool IsValid, string Error) Validate()
    {
        #region Basic validations

        if (!Enabled) return (false, "The schedule is disabled.");

        if (!Enum.IsDefined(Type)) return (false, "Not defined schedule type.");

        if (!Enum.IsDefined(Occurs)) return (false, "Not defined occurs type.");

        if (RecursEvery <= 0) return (false, "The Every value must be greater than 0.");

        if (LimitsStartDateLocal.HasValue && LimitsEndDateLocal.HasValue && LimitsStartDateLocal > LimitsEndDateLocal)
            return (false, "Within the limits, the start date cannot be later than the end date.");

        if (string.IsNullOrWhiteSpace(TimeZoneId)) return (false, "The TimeZoneId is required.");

        if (string.IsNullOrWhiteSpace(Locale)) return (false, "The Locale is required.");

        if (FirstDayOfWeek.HasValue && !Enum.IsDefined(FirstDayOfWeek.Value))
            return (false, "The provided FirstDayOfWeek is not a valid day of the week.");

        #endregion Basic validations

        #region Object Values integrity validations

        if (DailyFrequencyConfiguration?.Validate() is { IsValid: false } valDaily) return valDaily;
        if (WeeklyConfiguration?.Validate() is { IsValid: false } valWeekly) return valWeekly;
        if (MonthlyConfiguration?.Validate() is { IsValid: false } valMonthly) return valMonthly;

        #endregion Object Values integrity validations   

        return (true, string.Empty);
    }

}