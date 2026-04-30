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

        if (config.Every < 0)
            return (false, "The Every value cannot be negative.");

        if (!TryGetTimeZone(config.TimeZoneId, out var timeZone))
            return (false, $"Invalid TimeZoneId: {config.TimeZoneId}");

        ///// Additional validation based on Occurs type
        //if (config.Occurs == OccursType.Weekly && config.Weekly is null)
        //    return (false, "Weekly configuration is required for Weekly occurs type.");

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