using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class WeeklyCalendarRule
{
    public static bool IsValidDay(DateOnly day, DateOnly startDate, WeeklySchedule schedule)
    {
        if (!schedule.DaysOfWeek.Contains(day.DayOfWeek))
            return false;

        var startWeek = GetWeekIndex(startDate);
        var currentWeek = GetWeekIndex(day);

        var weeksDifference = currentWeek - startWeek;

        return weeksDifference >= 0 && weeksDifference % schedule.EveryWeeks == 0;
    }

    private static int GetWeekIndex(DateOnly date)
    {
        // Week index basado en lunes como inicio de semana
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) 
            dayOfWeek = 7; // Sunday -> 7

        // Día absoluto de inicio de la semana
        var startOfWeek = date.DayNumber - (dayOfWeek - 1);

        return startOfWeek / 7;
    }
}