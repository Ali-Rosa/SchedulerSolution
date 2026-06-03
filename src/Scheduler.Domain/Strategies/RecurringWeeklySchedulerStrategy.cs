using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);
        var firstDayOfWeek = CultureRule.GetFirstDayOfWeek(config);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => WeeklyCalendarRule.GetNextValidDay(fromDay, startDay, config.WeeklyConfiguration!.DaysOfWeek, config.RecursEvery, firstDayOfWeek),
            (nextDate) => {
                var prefix = localizer.BuildWeeklyPrefix(config.WeeklyConfiguration!.DaysOfWeek, config.RecursEvery);
                return localizer.BuildFullDescription(prefix, nextDate, config, timeZone);
            }
        );
    }
}