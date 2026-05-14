namespace Scheduler.Domain.Models;

public sealed record SchedulerWeekly (
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
);