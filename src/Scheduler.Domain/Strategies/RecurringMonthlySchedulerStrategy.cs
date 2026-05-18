using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringMonthlySchedulerStrategy : ISchedulerStrategy
{
    public SchedulerStrategyKey Key => new(SchedulerType.Recurring, SchedulerOccursType.Monthly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {

        if (config.RecursEvery <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

        if (config.Monthly == null)
            return new SchedulerResponse("Monthly configuration is required for Monthly recurring schedules.");

        if (!config.Monthly.IsSpecificDay)
        {
            if (config.Monthly.RelativeDayType.HasValue && !Enum.IsDefined(config.Monthly.RelativeDayType.Value))
                return new SchedulerResponse($"Not defined relative day type: {config.Monthly.RelativeDayType}.");

            if (config.Monthly.RelativeOrdinal.HasValue && !Enum.IsDefined(config.Monthly.RelativeOrdinal.Value))
                return new SchedulerResponse($"Not defined relative ordinal: {config.Monthly.RelativeOrdinal}.");
        }

        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            currentDateUtc
            , config
            , timeZone
            , 1
            , (currentDay, startDay) => {
                return MonthlyCalendarRule.IsValidDay(currentDay, startDay, config.RecursEvery, config.Monthly);
            }
            , (nextDate) => {
                var prefix = BuildMonthlyDescription(config.Monthly, config.RecursEvery);
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

    private static string FormatDayType(SchedulerMonthlyRelativeDayType dayType)
    {
        return dayType switch
        {
            SchedulerMonthlyRelativeDayType.WeekendDay => "weekend day",
            SchedulerMonthlyRelativeDayType.Weekday => "weekday",
            SchedulerMonthlyRelativeDayType.Day => "day",
            _ => dayType.ToString().ToLower()
        };
    }
}
