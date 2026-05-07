using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Scheduler.Domain.Strategies;

public sealed class RecurringDailyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.RecursEvery <= 0) 
            return new SchedulerResponse("The Every value must be greater than 0.");

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc
            , config
            , timeZone, 1
            , (currentDay, startDay) => {return DailyCalendarRule.IsValidDay(currentDay, startDay, config.RecursEvery);}
            , (nextDate) => {
                var prefix = config.RecursEvery == 1 ? "Occurs every day. " : $"Occurs every {config.RecursEvery} days. ";
                return DescriptionRule.Format(prefix, nextDate, config, timeZone);
            }
        );
    }
}
