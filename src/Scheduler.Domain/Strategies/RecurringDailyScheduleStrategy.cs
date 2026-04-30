using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringDailyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.Every <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

        var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        var seriesStartLocal = TimeZoneInfo.ConvertTime(config.StartDateLocal ?? config.ExecutionDateTimeLocal ?? currentDateUtc, timeZone);

        var searchCursorLocal = currentLocal > seriesStartLocal ? currentLocal : seriesStartLocal;

        for (int i = 0; i < 366; i++)
        {
            var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);
            var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);

            if (IsDayValid(currentDay, seriesStartDay, config.Every))
            {
                var dayExecutions = GetExecutionsForDay(currentDay, config, timeZone);

                // primera ejecucion del rango permitido
                var nextExecution = dayExecutions
                    .Where(e => e > currentDateUtc)
                    .Where(e => !config.EndDateLocal.HasValue || e <= config.EndDateLocal.Value)
                    .OrderBy(e => e)
                    .FirstOrDefault();

                if (nextExecution != default)
                {
                    return new SchedulerResponse(nextExecution, BuildDescription(nextExecution, config, timeZone));
                }
            }

            // si supero la fecha fin detengo el bucle
            if (config.EndDateLocal.HasValue && searchCursorLocal > config.EndDateLocal.Value)
                break;

            // Avanzamos al siguiente día (a las 00:00:00 local)
            searchCursorLocal = new DateTimeOffset(currentDay.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone.GetUtcOffset(searchCursorLocal.DateTime));
        }

        return new SchedulerResponse("No valid daily execution found within the allowed range.");
    }

    private bool IsDayValid(DateOnly currentDay, DateOnly startDay, int every)
    {
        // calculo matematico basandome en el numero absoluto de dias
        int diff = currentDay.DayNumber - startDay.DayNumber;
        return diff >= 0 && diff % every == 0;
    }

    private IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        // Si existe configuraCION horaria IntraDay
        if (config.IntraDay is not null)
        {
            return IntraDayRule.GetExecutionsForDay(day, config.IntraDay, timeZone);
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
        var everyDesc = config.Every == 1 ? "day" : $"{config.Every} days";

        var desc = $"Occurs every {everyDesc}. ";

        if (config.IntraDay != null)
            desc += $"Every {config.IntraDay.Every} {config.IntraDay.Unit.ToString().ToLower()} ";

        desc += $"at {local:HH:mm}. Starting on {nextExecution:dd/MM/yyyy}";

        return desc;
    }
}