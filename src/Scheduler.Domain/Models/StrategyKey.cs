using System.Diagnostics.CodeAnalysis;

namespace Scheduler.Domain.Models;

[ExcludeFromCodeCoverage]
public readonly record struct StrategyKey (
    SchedulerType ScheduleType,
    OccursType OccursType
);
