
namespace Scheduler.Domain.Rules;

public static class WeeklyCalendarRule
{
    public static bool IsValidDay(DateOnly day, DateOnly startDate, IReadOnlyCollection<DayOfWeek> daysOfWeek, int everyWeeks, DayOfWeek firstDayOfWeek)
    {
        if (!daysOfWeek.Contains(day.DayOfWeek))
            return false;

        var startWeek = GetWeekIndex(startDate, firstDayOfWeek);
        var currentWeek = GetWeekIndex(day, firstDayOfWeek);

        var weeksDifference = currentWeek - startWeek;

        return weeksDifference >= 0 && weeksDifference % everyWeeks == 0;
    }

    private static int GetWeekIndex(DateOnly date, DayOfWeek firstDayOfWeek)
    {
        int currentDayVal = (int)date.DayOfWeek;
        int firstDayVal = (int)firstDayOfWeek;

        int daysSinceStartOfWeek = (currentDayVal - firstDayVal + 7) % 7;

        int startOfWeekDayNumber = date.DayNumber - daysSinceStartOfWeek;

        return startOfWeekDayNumber / 7;
    }
}