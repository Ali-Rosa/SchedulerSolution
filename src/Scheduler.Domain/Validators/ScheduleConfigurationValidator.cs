using Scheduler.Domain.Models;

namespace Scheduler.Domain.Validators;

public static class ScheduleConfigurationValidator
{
    public static (bool IsValid, string ErrorMessage) Validate(ScheduleConfiguration config)
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

        if (config.RecursEvery < 0)
            return (false, "The Every value cannot be negative.");

        if (!TryGetTimeZone(config.TimeZoneId, out var timeZone))
            return (false, $"Invalid TimeZoneId: {config.TimeZoneId}");

        if (config.Type == ScheduleType.Recurring && config.Occurs == OccursType.Weekly && config.Weekly is null)
            return (false, "Weekly configuration is required for Weekly recurring schedules.");

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