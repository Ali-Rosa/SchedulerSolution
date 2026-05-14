using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

public interface ISchedulerStrategy
{
    SchedulerStrategyKey Key { get; }
    SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, SchedulerConfiguration config, TimeZoneInfo timeZone);
}