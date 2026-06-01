namespace Scheduler.Domain.Rules;

public static class DailyCalendarRule
{
    public static DateOnly? GetNextValidDay(DateOnly fromDay, DateOnly startDate, int everyDays)
    {
        if (everyDays <= 0) return null;

        int diff = fromDay.DayNumber - startDate.DayNumber;

        // If the query date is earlier than or equal to the start date, the first execution is the start date.
        if (diff <= 0) return startDate;

        // Integer division with rounding up to determine how many 'everyDays' intervals
        // we need to add to reach or surpass 'fromDay'
        int intervals = (diff + everyDays - 1) / everyDays;

        return startDate.AddDays(intervals * everyDays);
    }
}