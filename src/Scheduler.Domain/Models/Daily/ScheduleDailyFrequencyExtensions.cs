using Scheduler.Domain.Localization;

namespace Scheduler.Domain.Models.Daily;

public static class ScheduleDailyFrequencyExtensions
{
    public static (bool IsValid, string Error) Validate(this ScheduleDailyFrequency config, ISchedulerLocalizer localizer)
    {
        if (config.OccursEveryEnable)
        {
            if (!Enum.IsDefined(config.IntervalUnit))
               return (false, localizer.GetValidationError(ValidationErrorKey.InvalidIntervalUnit));

            if (config.FrequencyInterval <= 0)
                return (false, localizer.GetValidationError(ValidationErrorKey.FrequencyIntervalMustBePositive));
        }
    
        return (true, string.Empty);
    }
}
