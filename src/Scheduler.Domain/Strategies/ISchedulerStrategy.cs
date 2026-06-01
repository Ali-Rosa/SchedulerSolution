using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

public interface ISchedulerStrategy
{
    StrategyKey Key { get; }
    SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone);
}