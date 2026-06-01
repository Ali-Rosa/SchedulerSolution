using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Rules;
using System.ComponentModel;
using System.Reflection;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringMonthlySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Recurring, OccursType.Monthly);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var cultureInfo = CultureRule.GetCultureInfo(config.Locale!);

        return ScheduleEngine.IterateAndCalculate(
            config,
            timeZone,
            (fromDay, startDay) => MonthlyCalendarRule.GetNextValidDay(fromDay, startDay, config.RecursEvery, config.MonthlyConfiguration!),
            (nextDate) => {
                var prefix = BuildMonthlyDescription(config.MonthlyConfiguration!, config.RecursEvery);
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
        FieldInfo? field = dayType.GetType().GetField(dayType.ToString());
        if (field != null)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null) return attribute.Description; 
        }
        return dayType.ToString().ToLower();
    }
}
