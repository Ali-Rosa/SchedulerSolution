namespace Scheduler.Domain.Models.Weekly;

public sealed record SchedulerWeekly
{
    public IReadOnlyCollection<DayOfWeek> DaysOfWeek { get; init; } = Array.Empty<DayOfWeek>();

    public (bool IsValid, string Error) Validate()
    {
        if (DaysOfWeek == null || DaysOfWeek.Count == 0)
            return (false, "Weekly configuration requires at least one day.");

        return (true, string.Empty);
    }

}