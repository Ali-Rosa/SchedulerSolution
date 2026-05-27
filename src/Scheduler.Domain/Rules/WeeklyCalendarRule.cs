namespace Scheduler.Domain.Rules;

public static class WeeklyCalendarRule
{
    public static DateOnly? GetNextValidDay(DateOnly fromDay, DateOnly startDate, IReadOnlyCollection<DayOfWeek> daysOfWeek, int everyWeeks, DayOfWeek firstDayOfWeek)
    {
        int fromWeek = GetWeekIndex(fromDay, firstDayOfWeek);
        int startWeek = GetWeekIndex(startDate, firstDayOfWeek);

        int targetWeekIndex;
        if (fromWeek <= startWeek) targetWeekIndex = startWeek;
        else
        {
            int weekDiff = fromWeek - startWeek;
            int intervals = (weekDiff + everyWeeks - 1) / everyWeeks;
            targetWeekIndex = startWeek + intervals * everyWeeks;
        }

        while (true)
        {
            // Calculamos el inicio real de la semana aplicando el desplazamiento del firstDayOfWeek
            int dayOffset = ((int)firstDayOfWeek - 1 + 7) % 7;
            var weekStart = DateOnly.FromDayNumber(targetWeekIndex * 7 + dayOffset);

            // Buscamos los días activos de esa semana objetivo que sean iguales o posteriores a 'fromDay'
            var validDaysInWeek = Enumerable.Range(0, 7)
                .Select(offset => weekStart.AddDays(offset))
                .Where(day => daysOfWeek.Contains(day.DayOfWeek) && day >= fromDay)
                .OrderBy(day => day)
                .ToList();

            if (validDaysInWeek.Any()) return validDaysInWeek.First();

            // Si los días de la semana configurados ya ocurrieron, saltamos al siguiente ciclo
            targetWeekIndex += everyWeeks;
        }
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