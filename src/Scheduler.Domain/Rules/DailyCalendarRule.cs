namespace Scheduler.Domain.Rules;

public static class DailyCalendarRule
{
    public static DateOnly? GetNextValidDay(DateOnly fromDay, DateOnly startDate, int everyDays)
    {
        if (everyDays <= 0) return null;

        int diff = fromDay.DayNumber - startDate.DayNumber;

        // Si la fecha de consulta es anterior o igual al inicio, la primera ejecución es la de inicio
        if (diff <= 0) return startDate;

        // División entera con redondeo hacia arriba para saber cuántos intervalos de 'everyDays' 
        // necesitamos añadir para alcanzar o superar a 'fromDay'
        int intervals = (diff + everyDays - 1) / everyDays;

        return startDate.AddDays(intervals * everyDays);
    }
}