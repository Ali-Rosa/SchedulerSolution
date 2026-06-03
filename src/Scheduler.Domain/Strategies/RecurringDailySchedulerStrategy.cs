using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringDailySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => DailyCalendarRule.GetNextValidDay(fromDay, startDay, config.RecursEvery),
            (nextDate) => {
                var prefix = localizer.BuildDailyPrefix(config.RecursEvery);
                return localizer.BuildFullDescription(prefix, nextDate, config, timeZone);
            }
        );
    }
}