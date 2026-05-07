namespace Scheduler.Domain.Rules;

public static class DailyCalendarRule
{
    public static bool IsValidDay(DateOnly day, DateOnly startDate, int everyDays)
    {
        int diff = day.DayNumber - startDate.DayNumber;

        if (everyDays == 0) 
            return diff == 0;

        return diff >= 0 && diff % everyDays == 0;
    }
}