using Scheduler.Domain.Localization;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Models;

public static class SchedulerConfigurationExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerConfiguration config)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);

        #region Basic validations

        if (!config.Enabled) return (false, localizer.GetValidationError(ValidationErrorKey.ScheduleDisabled));

        if (!Enum.IsDefined(config.Type)) return (false, localizer.GetValidationError(ValidationErrorKey.UndefinedScheduleType));

        if (!Enum.IsDefined(config.Occurs)) return (false, localizer.GetValidationError(ValidationErrorKey.UndefinedOccursType));

        if (config.RecursEvery <= 0) return (false, localizer.GetValidationError(ValidationErrorKey.RecursEveryMustBePositive));

        if (config.LimitsStartDateLocal.HasValue && config.LimitsEndDateLocal.HasValue 
            && config.LimitsStartDateLocal > config.LimitsEndDateLocal)
            return (false, localizer.GetValidationError(ValidationErrorKey.StartDateLaterThanEndDate));

        if (string.IsNullOrWhiteSpace(config.TimeZoneId)) return (false, localizer.GetValidationError(ValidationErrorKey.TimeZoneRequired));

        if (string.IsNullOrWhiteSpace(config.Locale)) return (false, localizer.GetValidationError(ValidationErrorKey.LocaleRequired));

        if (config.FirstDayOfWeek.HasValue && !Enum.IsDefined(config.FirstDayOfWeek.Value))
            return (false, localizer.GetValidationError(ValidationErrorKey.InvalidFirstDayOfWeek));

        #endregion Basic validations

        #region Object Values integrity validations

        if (config.Type == SchedulerType.Recurring && config.DailyFrequencyConfiguration is not null)
        {
            if (config.DailyFrequencyConfiguration.Validate(localizer) is { IsValid: false } valDaily)
                return valDaily;
        }

        if (config.Type == SchedulerType.Recurring && config.Occurs == OccursType.Weekly)
        {
            if (config.WeeklyConfiguration is null)
                return (false, localizer.GetValidationError(ValidationErrorKey.WeeklyConfigRequired));

            if (config.WeeklyConfiguration.Validate(localizer) is { IsValid: false } valWeekly)
                return valWeekly;
        }

        if (config.Type == SchedulerType.Recurring && config.Occurs == OccursType.Monthly)
        {
            if (config.MonthlyConfiguration is null)
                return (false, localizer.GetValidationError(ValidationErrorKey.MonthlyConfigRequired));

            if (config.MonthlyConfiguration.Validate(localizer) is { IsValid: false } valMonthly)
                return valMonthly;
        }

        #endregion Object Values integrity validations   

        return (true, string.Empty);
    }
}
