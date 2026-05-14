using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Tests.TestHelpers.Factories;

internal static class SchedulerServiceFactory
{
    public static SchedulerService CreateDefault()
    {
        return new SchedulerService(new ISchedulerStrategy[]
        {
            new OnceDailySchedulerStrategy(),
            new RecurringDailySchedulerStrategy(),
            new RecurringWeeklySchedulerStrategy(),
            new RecurringMonthlySchedulerStrategy()
        });
    }
}