namespace Scheduler.Domain.Models;

public record ScheduleConfiguration(
    ScheduleType Type,
    DateTime ExecutionDateTime,
    OccursType Occurs,
    bool Enabled,
    int Every,
    DateTime StartDate,
    DateTime? EndDate   
);