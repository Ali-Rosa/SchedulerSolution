using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Models;

public record SchedulerConfiguration (
    bool Enabled,
    SchedulerType Type,
    DateTimeOffset? ExecutionDateTimeLocal,
    OccursType Occurs,
    int RecursEvery,
    DateTimeOffset? LimitsStartDateLocal,
    DateTimeOffset? LimitsEndDateLocal,
    string TimeZoneId,
    string Locale,
    DayOfWeek? FirstDayOfWeek = null,
    ScheduleDailyFrequency? DailyFrequencyConfiguration = null,
    SchedulerWeekly? WeeklyConfiguration = null,
    SchedulerMonthly? MonthlyConfiguration = null
)
{
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
};