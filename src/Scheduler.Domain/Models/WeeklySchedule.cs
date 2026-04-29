
namespace Scheduler.Domain.Models;

public sealed record WeeklySchedule(
    int EveryWeeks,
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
);
