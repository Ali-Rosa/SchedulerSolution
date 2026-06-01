namespace Scheduler.Domain.Models.Weekly;

public static class SchedulerWeeklyExtensions
{
    public static (bool IsValid, string Error) Validate(this SchedulerWeekly config)
    {
        if (config.DaysOfWeek == null || config.DaysOfWeek.Count == 0)
            return (false, "Weekly configuration requires at least one day.");
    
        return (true, string.Empty);
    }
}
