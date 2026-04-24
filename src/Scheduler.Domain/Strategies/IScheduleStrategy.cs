using Scheduler.Domain.Models;
using System.Diagnostics.CodeAnalysis;

namespace Scheduler.Domain.Strategies
{
    public interface IScheduleStrategy
    {
        ScheduleStrategyKey Key { get; }
        SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, ScheduleConfiguration config, TimeZoneInfo timeZone);
    }
}