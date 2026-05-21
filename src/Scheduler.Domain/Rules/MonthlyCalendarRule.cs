using Scheduler.Domain.Models.Monthly;

namespace Scheduler.Domain.Rules;

public static class MonthlyCalendarRule
{
    public static DateOnly? GetNextValidDay(DateOnly fromDay, DateOnly startDate, int everyMonths, SchedulerMonthly monthlyConfig)
    {
        if (everyMonths <= 0) everyMonths = 1;

        // Diferencia de meses entre el inicio y la fecha de consulta
        int diffMonths = ((fromDay.Year - startDate.Year) * 12) + fromDay.Month - startDate.Month;

        int targetMonthOffset;
        if (diffMonths <= 0) 
            targetMonthOffset = 0;
        else
        {
            // Redondeo hacia arriba para encontrar el mes múltiplo de 'everyMonths'
            targetMonthOffset = ((diffMonths + everyMonths - 1) / everyMonths) * everyMonths;
        }

        while (true)
        {
            var targetMonthDate = startDate.AddMonths(targetMonthOffset);
            int year = targetMonthDate.Year;
            int month = targetMonthDate.Month;

            DateOnly? candidateDay = null;

            if (monthlyConfig.IsSpecificDay)
            {
                int targetDay = monthlyConfig.SpecificDayNumber!.Value;
                int daysInCurrentMonth = DateTime.DaysInMonth(year, month);

                // Controlamos si el día buscado existe en este mes específico
                if (targetDay <= daysInCurrentMonth) candidateDay = new DateOnly(year, month, targetDay);
            }
            else
            {
                candidateDay = GetRelativeDate(year, month, monthlyConfig.RelativeOrdinal!.Value, monthlyConfig.RelativeDayType!.Value);
            }

            // Si el día calculado en este mes es válido y es igual o posterior a 'fromDay', lo devolvemos
            if (candidateDay.HasValue && candidateDay.Value >= fromDay) return candidateDay.Value;

            // Si el día calculado para este mes ya pasó respecto a la fecha actual,
            // saltamos al siguiente ciclo de meses de forma analítica
            targetMonthOffset += everyMonths;
        }
    }

    private static DateOnly? GetRelativeDate(int year, int month, MonthlyRelativeOrdinal ordinal, MonthlyRelativeDayType dayType)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var matchingDays = new List<DateOnly>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (IsMatchingType(date, dayType)) matchingDays.Add(date);
        }

        if (!matchingDays.Any()) return null;

        if (ordinal == MonthlyRelativeOrdinal.Last) return matchingDays.Last();

        int index = (int)ordinal - 1;

        if (index >= 0 && index < matchingDays.Count) return matchingDays[index];

        return null;
    }

    private static bool IsMatchingType(DateOnly date, MonthlyRelativeDayType type)
    {
        return type switch
        {
            MonthlyRelativeDayType.Day => true,
            MonthlyRelativeDayType.Weekday => date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday,
            MonthlyRelativeDayType.WeekendDay => date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
            _ => (int)date.DayOfWeek == (int)type
        };
    }

}