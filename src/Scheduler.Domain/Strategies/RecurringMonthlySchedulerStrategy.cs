using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringMonthlySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Monthly);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => MonthlyCalendarRule.GetNextValidDay(fromDay, startDay, config.RecursEvery, config.MonthlyConfiguration!),
            (nextDate) => {
                var prefix = localizer.BuildMonthlyPrefix(config.MonthlyConfiguration!, config.RecursEvery);
                return localizer.BuildFullDescription(prefix, nextDate, config, timeZone);
            }
        );
    }
}
