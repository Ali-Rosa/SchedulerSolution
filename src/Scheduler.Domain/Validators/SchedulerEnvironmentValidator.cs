using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Validators;

public static class SchedulerEnvironmentValidator
{
    public static (bool IsValid, string Error, TimeZoneInfo? TimeZone) Validate(SchedulerConfiguration config)
    {
        if (!CultureRule.IsValid(config.Locale))
            return (false, $"The culture '{config.Locale}' is not supported by the system.", null);

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);
            return (true, string.Empty, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return (false, $"Invalid TimeZoneId: {config.TimeZoneId}", null);
        }
    }

}