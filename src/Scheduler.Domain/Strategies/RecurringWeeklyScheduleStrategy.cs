using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (!CultureRule.IsValid(config.Locale!))
            return new SchedulerResponse($"The culture '{config.Locale}' is not supported by the system.");

        if (config.Weekly!.DaysOfWeek == null || !config.Weekly.DaysOfWeek.Any())
        {
            return new SchedulerResponse("Weekly configuration and at least one day are required.");
        }

        if (config.RecursEvery <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

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