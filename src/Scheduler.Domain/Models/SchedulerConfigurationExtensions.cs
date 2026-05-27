using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Models;

public static class SchedulerConfigurationExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerConfiguration config)
    {
        #region Basic validations

        if (!config.Enabled) return (false, "The schedule is disabled.");

        if (!Enum.IsDefined(config.Type)) return (false, "Not defined schedule type.");

        if (!Enum.IsDefined(config.Occurs)) return (false, "Not defined occurs type.");

        if (config.RecursEvery <= 0) return (false, "The Every value must be greater than 0.");

        if (config.LimitsStartDateLocal.HasValue && config.LimitsEndDateLocal.HasValue 
            && config.LimitsStartDateLocal > config.LimitsEndDateLocal)
            return (false, "Within the limits, the start date cannot be later than the end date.");

        if (string.IsNullOrWhiteSpace(config.TimeZoneId)) return (false, "The TimeZoneId is required.");

        if (string.IsNullOrWhiteSpace(config.Locale)) return (false, "The Locale is required.");

        if (config.FirstDayOfWeek.HasValue && !Enum.IsDefined(config.FirstDayOfWeek.Value))
            return (false, "The provided FirstDayOfWeek is not a valid day of the week.");

        #endregion Basic validations

        #region Object Values integrity validations

        if (config.Type == SchedulerType.Recurring && config.DailyFrequencyConfiguration is not null)
        {
            if (config.DailyFrequencyConfiguration.Validate() is { IsValid: false } valDaily)
                return valDaily;
        }

        if (config.Type == SchedulerType.Recurring && config.Occurs == OccursType.Weekly)
        {
            if (config.WeeklyConfiguration is null)
                return (false, "Weekly configuration is required for Weekly recurring schedules.");

            if (config.WeeklyConfiguration.Validate() is { IsValid: false } valWeekly)
                return valWeekly;
        }

        if (config.Type == SchedulerType.Recurring && config.Occurs == OccursType.Monthly)
        {
            if (config.MonthlyConfiguration is null)
                return (false, "Monthly configuration is required for Monthly recurring schedules.");

            if (config.MonthlyConfiguration.Validate() is { IsValid: false } valMonthly)
                return valMonthly;
        }

        #endregion Object Values integrity validations   

        return (true, string.Empty);
    }
}
