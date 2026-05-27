namespace Scheduler.Domain.Models.Weekly;

public sealed record SchedulerWeekly
{
    public IReadOnlyCollection<DayOfWeek> DaysOfWeek { get; init; } = Array.Empty<DayOfWeek>();
}