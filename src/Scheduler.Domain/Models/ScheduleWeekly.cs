
namespace Scheduler.Domain.Models;

public sealed record ScheduleWeekly(
    int EveryWeeks,
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
);
