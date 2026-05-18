using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklySchedulerStrategy : ISchedulerStrategy
{
    public SchedulerStrategyKey Key => new(SchedulerType.Recurring, SchedulerOccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {

        if (config.RecursEvery <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

        if (config.Weekly is null)
            return new SchedulerResponse("Weekly configuration is required for Weekly recurring schedules.");

        if (config.Weekly.DaysOfWeek == null || !config.Weekly.DaysOfWeek.Any())
        {
            return new SchedulerResponse("Weekly configuration and at least one day are required.");
        }

        var firstDayOfWeek = CultureRule.GetFirstDayOfWeek(config);
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc
            , config
            , timeZone
            , 1
            , (currentDay, startDay) => {return WeeklyCalendarRule.IsValidDay( currentDay, startDay, config.Weekly.DaysOfWeek, config.RecursEvery, firstDayOfWeek);}
            , (nextDate) => {
                var days = string.Join(", ", config.Weekly.DaysOfWeek);
                var prefix = $"Occurs every {config.RecursEvery} week(s) on {days}. ";
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }
}