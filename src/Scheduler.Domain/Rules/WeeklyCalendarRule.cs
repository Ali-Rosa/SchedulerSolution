namespace Scheduler.Domain.Rules;

public static class WeeklyCalendarRule
{
    public static bool IsValidDay(DateOnly day, DateOnly startDate, IReadOnlyCollection<DayOfWeek> daysOfWeek, int everyWeeks)
    {
        // El dia de hoy esta entre los dias seleccionados?
        if (!daysOfWeek.Contains(day.DayOfWeek))
            return false;

        // Toca esta semana segun la frecuencia?
        var startWeek = GetWeekIndex(startDate);
        var currentWeek = GetWeekIndex(day);

        var weeksDifference = currentWeek - startWeek;

        return weeksDifference >= 0 && weeksDifference % everyWeeks == 0;
    }

    private static int GetWeekIndex(DateOnly date)
    {
        // lunes como inicio de semana
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7

        var startOfWeek = date.DayNumber - (dayOfWeek - 1);
        return startOfWeek / 7;
    }
}