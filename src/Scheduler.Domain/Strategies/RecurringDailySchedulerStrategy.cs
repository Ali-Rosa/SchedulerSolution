using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringDailySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => DailyCalendarRule.GetNextValidDay(fromDay, startDay, config.RecursEvery),
            (nextDate) => {
                var prefix = BuildDailyDescription(config.RecursEvery);
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }

    private static string BuildDailyDescription(int recursEvery)
    {
        return recursEvery == 1 ? "Occurs every day. " : $"Occurs every {recursEvery} days. ";
    }

}