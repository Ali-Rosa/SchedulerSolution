using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringMonthlySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Monthly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.MonthlyConfiguration == null)
            return new SchedulerResponse("Monthly configuration is required for Monthly recurring schedules.");

        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc,
            config,
            timeZone,
            (fromDay, startDay) => MonthlyCalendarRule.GetNextValidDay(fromDay, startDay, config.RecursEvery, config.MonthlyConfiguration),
            (nextDate) => {
                var prefix = BuildMonthlyDescription(config.MonthlyConfiguration, config.RecursEvery);
                return DescriptionRule.BuildExecutionDescription(prefix, nextDate, config, timeZone, cultureInfo);
            }
        );
    }

    private static string BuildMonthlyDescription(SchedulerMonthly monthly, int recursEvery)
    {
        string monthText = recursEvery == 1 ? "every month. " : $"every {recursEvery} months. ";

        if (monthly.IsSpecificDay)
            return $"Occurs day {monthly.SpecificDayNumber} of {monthText}";

        return $"Occurs the {monthly.RelativeOrdinal.ToString()!.ToLower()} {FormatDayType(monthly.RelativeDayType!.Value)} of {monthText}";
    }

    private static string FormatDayType(MonthlyRelativeDayType dayType)
    {
        return dayType switch
        {
            MonthlyRelativeDayType.WeekendDay => "weekend day",
            MonthlyRelativeDayType.Weekday => "weekday",
            MonthlyRelativeDayType.Day => "day",
            _ => dayType.ToString().ToLower()
        };
    }

}
