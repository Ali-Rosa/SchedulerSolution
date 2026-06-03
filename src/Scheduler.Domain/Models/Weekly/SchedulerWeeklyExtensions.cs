using Scheduler.Domain.Localization;

namespace Scheduler.Domain.Models.Weekly;

public static class SchedulerWeeklyExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerWeekly config, ISchedulerLocalizer localizer)
    {
        if (config.DaysOfWeek == null || config.DaysOfWeek.Count == 0)
            return (false, localizer.GetValidationError(ValidationErrorKey.WeeklyConfigMinDays));

        return (true, string.Empty);
    }
}
