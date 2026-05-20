using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.WeeklyConfiguration is null)
            return new SchedulerResponse("Weekly configuration is required for Weekly recurring schedules.");

        var firstDayOfWeek = CultureRule.GetFirstDayOfWeek(config);
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc
            , config
            , timeZone
            , 1
            , (currentDay, startDay) => {return WeeklyCalendarRule.IsValidDay( currentDay, startDay, config.WeeklyConfiguration.DaysOfWeek, config.RecursEvery, firstDayOfWeek);}
            , (nextDate) => {
                var days = string.Join(", ", config.WeeklyConfiguration.DaysOfWeek);
                var prefix = $"Occurs every {config.RecursEvery} week(s) on {days}. ";
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }
}