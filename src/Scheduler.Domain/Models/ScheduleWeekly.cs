namespace Scheduler.Domain.Models;

public sealed record ScheduleWeekly (
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
);