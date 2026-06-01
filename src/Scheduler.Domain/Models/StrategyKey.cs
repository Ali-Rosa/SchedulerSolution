namespace Scheduler.Domain.Models;

public readonly record struct StrategyKey (
    SchedulerType ScheduleType,
    OccursType OccursType
);
