using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringDailyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.RecursEvery <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

        // 1. El "Ahora" local
        var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        // 2. REGLA: El inicio de la serie SIEMPRE es el currentDateUtc (el día 06 en tu test)
        // Ignoramos ExecutionDateTimeLocal para el cálculo del patrón.
        var seriesStartLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        // 3. El cursor de búsqueda empieza en el máximo entre "Hoy" y el "Límite de inicio"
        var limitStartLocal = config.LimitsStartDateLocal.HasValue
            ? TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal.Value, timeZone)
            : seriesStartLocal;

        var searchCursorLocal = currentLocal > limitStartLocal ? currentLocal : limitStartLocal;

        // 4. Tope de seguridad (366 días) para evitar bucles infinitos
        for (int i = 0; i < 366; i++)
        {
            var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);
            var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);

            // REGLA DEL PROFESOR: diff >= 0 (El primer día, día 0, es válido)
            if (IsDayValid(currentDay, seriesStartDay, config.RecursEvery))
            {
                var dayExecutions = GetExecutionsForDay(currentDay, config, timeZone);

                // Buscamos la primera ejecución que sea estrictamente futura y esté en límites
                var nextExecution = dayExecutions
                    .Where(e => e > currentDateUtc)
                    .Where(e => !config.LimitsEndDateLocal.HasValue || e <= config.LimitsEndDateLocal.Value)
                    .OrderBy(e => e)
                    .FirstOrDefault();

                if (nextExecution != default)
                {
                    // Validación extra: Que la ejecución encontrada no sea anterior al límite de inicio
                    if (!config.LimitsStartDateLocal.HasValue || nextExecution >= config.LimitsStartDateLocal.Value)
                    {
                        return new SchedulerResponse(nextExecution, BuildDescription(nextExecution, config, timeZone));
                    }
                }
            }

            // Si superamos el límite final, dejamos de buscar
            if (config.LimitsEndDateLocal.HasValue && searchCursorLocal > config.LimitsEndDateLocal.Value)
                break;

            // Avanzamos al siguiente día a las 00:00 local
            searchCursorLocal = new DateTimeOffset(currentDay.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone.GetUtcOffset(searchCursorLocal.DateTime));
        }

        return new SchedulerResponse("No valid daily execution found within the allowed range.");
    }

    private bool IsDayValid(DateOnly currentDay, DateOnly startDay, int every)
    {
        int diff = currentDay.DayNumber - startDay.DayNumber;
        // diff >= 0 permite que el día 06 sea el primer día de la serie
        return diff >= 0 && diff % every == 0;
    }












    //public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    //{
    //    if (config.RecursEvery <= 0)
    //        return new SchedulerResponse("The Every value must be greater than 0.");

    //    var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

    //    var seriesStartLocal = TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal ?? config.ExecutionDateTimeLocal ?? currentDateUtc, timeZone);

    //    var searchCursorLocal = currentLocal > seriesStartLocal ? currentLocal : seriesStartLocal;

    //    for (int i = 0; i < 366; i++)
    //    {
    //        var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);
    //        var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);

    //        if (IsDayValid(currentDay, seriesStartDay, config.RecursEvery))
    //        {
    //            var dayExecutions = GetExecutionsForDay(currentDay, config, timeZone);

    //            // primera ejecucion del rango permitido
    //            var nextExecution = dayExecutions
    //                .Where(e => e > currentDateUtc)
    //                .Where(e => !config.LimitsEndDateLocal.HasValue || e <= config.LimitsEndDateLocal.Value)
    //                .OrderBy(e => e)
    //                .FirstOrDefault();

    //            if (nextExecution != default)
    //            {
    //                return new SchedulerResponse(nextExecution, BuildDescription(nextExecution, config, timeZone));
    //            }
    //        }

    //        // si supero la fecha fin detengo el bucle
    //        if (config.LimitsEndDateLocal.HasValue && searchCursorLocal > config.LimitsEndDateLocal.Value)
    //            break;

    //        // Avanzamos al siguiente día (a las 00:00:00 local)
    //        searchCursorLocal = new DateTimeOffset(currentDay.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone.GetUtcOffset(searchCursorLocal.DateTime));
    //    }

    //    return new SchedulerResponse("No valid daily execution found within the allowed range.");
    //}

    //private bool IsDayValid(DateOnly currentDay, DateOnly startDay, int every)
    //{
    //    // calculo matematico basandome en el numero absoluto de dias
    //    int diff = currentDay.DayNumber - startDay.DayNumber;
    //    return diff >= 0 && diff % every == 0;
    //}

    private IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        // Si existe configuraCION horaria IntraDay
        if (config.DailyFrecuency is not null)
        {
            return DailyFrecuencyRule.GetExecutionsForDay(day, config.DailyFrecuency, timeZone);
        }

        // SI ES UNA EJECUCION NORMAL COMO LA PARTE 1 DEL EJERCICIO HACEMOS ESTO
        // Usamos la hora de ExecutionDateTimeLocal o por defecto las 00:00
        var time = config.ExecutionDateTimeLocal?.TimeOfDay ?? TimeSpan.Zero;
        var localDateTime = day.ToDateTime(TimeOnly.FromTimeSpan(time));

        return new[] { new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime)) };
    }

    private string BuildDescription(DateTimeOffset nextExecution, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var everyDesc = config.RecursEvery == 1 ? "day" : $"{config.RecursEvery} days";

        var desc = $"Occurs every {everyDesc}. ";

        if (config.DailyFrecuency != null)
            desc += $"Every {config.DailyFrecuency.FrequencyInterval} {config.DailyFrecuency.IntervalUnit.ToString().ToLower()} ";

        desc += $"at {local:HH:mm}. Starting on {nextExecution:dd/MM/yyyy}";

        return desc;
    }
}