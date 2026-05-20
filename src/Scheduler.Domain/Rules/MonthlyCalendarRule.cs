using Scheduler.Domain.Models.Monthly;

namespace Scheduler.Domain.Rules;

public static class MonthlyCalendarRule
{
    public static bool IsValidDay(DateOnly currentDay, DateOnly startDate, int everyMonths, SchedulerMonthly monthlyConfig)
    {
        // The total difference in months between the start date and the current date is calculated
        int monthsDiff = ((currentDay.Year - startDate.Year) * 12) + currentDay.Month - startDate.Month;

        // If the current month is earlier than the start date or does not match the multiple of months, it is invalid
        if (monthsDiff < 0 || monthsDiff % everyMonths != 0)
            return false;

        // Logic for "Specific Day" (e.g., the 8th day of each month)
        if (monthlyConfig.IsSpecificDay)
        {
            int targetDay = monthlyConfig.SpecificDayNumber!.Value;
            int daysInCurrentMonth = DateTime.DaysInMonth(currentDay.Year, currentDay.Month);

            // Protection: If the requested day is 31 in February, adjust it to 28/29 (End of month)
            // This possibility must be eradicated at a higher stage, possibly in validations; NO OLVIDAR
            //if (targetDay > daysInCurrentMonth)
            //    targetDay = daysInCurrentMonth;

            if (targetDay > daysInCurrentMonth)
                return false; // Salimos, no hay ejecución este mes

            return currentDay.Day == targetDay;
        }

        // Logic for "Relative Day" (e.g., The First Thursday, The Second Weekend)
        var calculatedRelativeDate = GetRelativeDate(currentDay.Year, currentDay.Month, monthlyConfig.RelativeOrdinal!.Value, monthlyConfig.RelativeDayType!.Value);

        return calculatedRelativeDate.HasValue && currentDay == calculatedRelativeDate.Value;

    }

    private static DateOnly? GetRelativeDate(int year, int month, MonthlyRelativeOrdinal ordinal, MonthlyRelativeDayType dayType)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var matchingDays = new List<DateOnly>();

        // Collect all days of this month that match the condition (e.g., All Thursdays)
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (IsMatchingType(date, dayType))
            {
                matchingDays.Add(date);
            }
        }

        if (!matchingDays.Any()) return null;

        // If it's the "Last", return the last in the collected list
        if (ordinal == MonthlyRelativeOrdinal.Last)
            return matchingDays.Last();

        // For First (1), Second (2), etc... subtract 1 to get the array index (0, 1, 2...)
        int index = (int)ordinal - 1;

        if (index >= 0 && index < matchingDays.Count)
            return matchingDays[index];

        return null; 

    }

    private static bool IsMatchingType(DateOnly date, MonthlyRelativeDayType type)
    {
        return type switch
        {
            MonthlyRelativeDayType.Day => true,
            MonthlyRelativeDayType.Weekday => date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday,
            MonthlyRelativeDayType.WeekendDay => date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
            _ => (int)date.DayOfWeek == (int)type // We take advantage of the fact that 0-6 matches System.DayOfWeek
        };
    }
}
