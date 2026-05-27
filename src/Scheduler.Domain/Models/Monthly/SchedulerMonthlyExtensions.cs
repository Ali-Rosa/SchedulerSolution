namespace Scheduler.Domain.Models.Monthly;

public static class SchedulerMonthlyExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerMonthly config)
    {
        if (config.IsSpecificDay)
        {
            if (!config.SpecificDayNumber.HasValue || config.SpecificDayNumber < 1 || config.SpecificDayNumber > 31)
                return (false, "The day must be between 1 and 31.");
        }
        else
        {
            if (!config.RelativeOrdinal.HasValue || !Enum.IsDefined(config.RelativeOrdinal.Value))
                return (false, $"Not defined relative ordinal: {config.RelativeOrdinal}.");
    
            if (!config.RelativeDayType.HasValue || !Enum.IsDefined(config.RelativeDayType.Value))
                return (false, $"Not defined relative day type: {config.RelativeDayType}.");
        }
    
        return (true, string.Empty);
    }
}
