namespace Scheduler.Domain.Models;

public readonly record struct ScheduleStrategyKey(
    ScheduleType ScheduleType,
    OccursType OccursType
);
