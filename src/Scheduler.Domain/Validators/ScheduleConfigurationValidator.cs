using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Validators;

public static class ScheduleConfigurationValidator
{
    public static (bool IsValid, string ErrorMessage) Validate(SchedulerConfiguration config)
    {
        if (config is null)
            return (false, "The configuration cannot be null.");

        if (!config.Enabled)
            return (false, "The schedule is disabled.");

        if (!Enum.IsDefined(config.Type))
            return (false, "Not defined schedule type.");

        if (!Enum.IsDefined(config.Occurs))
            return (false, "Not defined occurs type.");

        if (string.IsNullOrWhiteSpace(config.Locale))
            return (false, "The Locale is required.");

        if (!CultureRule.IsValid(config.Locale))
            return (false, $"The culture '{config.Locale}' is not supported by the system.");

        if (config.RecursEvery < 0)
            return (false, "The Every value cannot be negative.");

        if (!TryGetTimeZone(config.TimeZoneId, out var timeZone))
            return (false, $"Invalid TimeZoneId: {config.TimeZoneId}");

        if (config.DailyFrequency != null && (config.DailyFrequency.OccursEveryEnable && !config.DailyFrequency.OccursOnceEnable))
        {
            if (!Enum.IsDefined(config.DailyFrequency.IntervalUnit))
                return (false, "Not defined interval unit for daily frequency.");

            if (config.DailyFrequency.FrequencyInterval <= 0)
                return (false, "The frequency interval must be greater than 0.");
        }

        return (true, string.Empty);
    }

    private static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo? timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = null;
            return false;
        }
    }

}