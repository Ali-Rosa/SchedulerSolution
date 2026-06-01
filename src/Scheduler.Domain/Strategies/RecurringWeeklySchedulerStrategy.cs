using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Weekly;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var firstDayOfWeek = CultureRule.GetFirstDayOfWeek(config);
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => WeeklyCalendarRule.GetNextValidDay(fromDay, startDay, config.WeeklyConfiguration!.DaysOfWeek, config.RecursEvery, firstDayOfWeek),
            (nextDate) => {
                var prefix = BuildWeeklyDescription(config.WeeklyConfiguration!, config.RecursEvery);
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }
    private static string BuildWeeklyDescription(SchedulerWeekly weekly, int recursEvery)
    {
        var days = string.Join(", ", weekly.DaysOfWeek);
        
        string weekText = recursEvery == 1 ? "week" : $"{recursEvery} weeks";

        return $"Occurs every {weekText} on {days}. ";
    }

}