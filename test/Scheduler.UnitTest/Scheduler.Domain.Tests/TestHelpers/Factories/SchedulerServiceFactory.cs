using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Tests.TestHelpers.Factories;

internal static class SchedulerServiceFactory
{
    public static SchedulerService CreateDefault()
    {
        return new SchedulerService(new IScheduleStrategy[]
        {
            new OnceScheduleStrategy(),
            new RecurringScheduleStrategy(),
            new WeeklyScheduleStrategy()
        });
    }
}