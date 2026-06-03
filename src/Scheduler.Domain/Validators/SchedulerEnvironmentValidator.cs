using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Validators;

public static class SchedulerEnvironmentValidator
{
    public static (bool IsValid, string Error, TimeZoneInfo? TimeZone) Validate(SchedulerConfiguration config)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);

        if (!CultureRule.IsValid(config.Locale))
            return (false, localizer.GetValidationError(ValidationErrorKey.CultureNotSupported, config.Locale), null);

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);
            return (true, string.Empty, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return (false, localizer.GetValidationError(ValidationErrorKey.InvalidTimeZone, config.TimeZoneId), null);
        }
    }
}