using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies
{
    public interface IScheduleStrategy
    {
        ScheduleType Type { get; }
        SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, DateTimeOffset currentLocalTime, ScheduleConfiguration config);
    }
}