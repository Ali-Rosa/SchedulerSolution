using Scheduler.Domain.Localization;

namespace Scheduler.Domain.Models.Monthly;

public static class SchedulerMonthlyExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerMonthly config, ISchedulerLocalizer localizer)
    {
        if (config.IsSpecificDay)
        {
            if (!config.SpecificDayNumber.HasValue || config.SpecificDayNumber < 1 || config.SpecificDayNumber > 31)
                return (false, localizer.GetValidationError(ValidationErrorKey.InvalidMonthlyDay));
        }
        else
        {
            if (!config.RelativeOrdinal.HasValue || !Enum.IsDefined(config.RelativeOrdinal.Value))
                return (false, localizer.GetValidationError(ValidationErrorKey.UndefinedRelativeOrdinal, config.RelativeOrdinal!));

            if (!config.RelativeDayType.HasValue || !Enum.IsDefined(config.RelativeDayType.Value))
                return (false, localizer.GetValidationError(ValidationErrorKey.UndefinedRelativeDayType, config.RelativeDayType!));
        }
    
        return (true, string.Empty);
    }
}
