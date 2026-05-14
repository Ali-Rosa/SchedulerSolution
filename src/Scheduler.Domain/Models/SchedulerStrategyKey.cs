namespace Scheduler.Domain.Models;

public readonly record struct SchedulerStrategyKey (
    SchedulerType ScheduleType,
    SchedulerOccursType OccursType
);
