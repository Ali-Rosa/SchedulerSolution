using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringDailySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc
            , config
            , timeZone
            , 1
            , (currentDay, startDay) => {return DailyCalendarRule.IsValidDay(currentDay, startDay, config.RecursEvery);}
            , (nextDate) => {
                var prefix = config.RecursEvery == 1 ? "Occurs every day. " : $"Occurs every {config.RecursEvery} days. ";
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }
}