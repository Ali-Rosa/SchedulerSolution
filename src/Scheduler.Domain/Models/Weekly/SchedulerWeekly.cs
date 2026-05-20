namespace Scheduler.Domain.Models.Weekly;

public sealed record SchedulerWeekly (
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
)
{   
    public (bool IsValid, string Error) Validate()
    {
        if (DaysOfWeek == null || DaysOfWeek.Count == 0) return (false, "Weekly configuration requires at least one day.");

        return (true, string.Empty);
    }
};