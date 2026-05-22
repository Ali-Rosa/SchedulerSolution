using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class ScheduleEngine
{
    public static SchedulerResponse IterateAndCalculate(
        DateTimeOffset currentDateUtc
        , SchedulerConfiguration config
        , TimeZoneInfo timeZone
        , Func<DateOnly, DateOnly, DateOnly?> getNextValidDayLogic
        , Func<DateTimeOffset, string> buildDescriptionLogic
        , int maxOccurrences = 1
    )
    {
        var results = new List<DateTimeOffset>();
        var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        // Corregimos el punto de anclaje de la serie para que sea la fecha de inicio de límites
        // o en su defecto, el inicio de la evaluación actual.
        var seriesStartLocal = config.LimitsStartDateLocal.HasValue
            ? TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal.Value, timeZone)
            : currentLocal;

        var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);
        var anchorTime = seriesStartLocal.TimeOfDay;

        var searchCursorLocal = currentLocal > seriesStartLocal ? currentLocal : seriesStartLocal;
        var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);

        // Saltamos de fecha válida en fecha válida de forma directa.
        while (results.Count < maxOccurrences)
        {
            var nextValidDay = getNextValidDayLogic(currentDay, seriesStartDay);

            if (!nextValidDay.HasValue) break;

            // Si el día calculado supera el límite final establecido, terminamos
            if (config.LimitsEndDateLocal.HasValue)
            {
                var limitEndLocal = TimeZoneInfo.ConvertTime(config.LimitsEndDateLocal.Value, timeZone);
                if (nextValidDay.Value > DateOnly.FromDateTime(limitEndLocal.DateTime)) break;
            }

            var executions = GetExecutionsForDay(nextValidDay.Value, currentDateUtc, config, timeZone, anchorTime);

            foreach (var execution in executions)
            {
                if (results.Count < maxOccurrences) results.Add(execution);
            }

            // Movemos el cursor al día siguiente del calculado para buscar la posterior ocurrencia
            currentDay = nextValidDay.Value.AddDays(1);
        }

        if (results.Count == 0) return new SchedulerResponse("No valid executions were found within the limits with this configuration.");

        string description = buildDescriptionLogic(results.First());

        return new SchedulerResponse(results, description);
    }

    private static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, DateTimeOffset currentDateUtc, SchedulerConfiguration config, TimeZoneInfo timeZone, TimeSpan anchorTime)
    {
        var allDayExecutions = (config.DailyFrequencyConfiguration != null)
            ? DailyFrequencyRule.GetExecutionsForDay(day, config.DailyFrequencyConfiguration, timeZone)
            : GenerateSingleExecution(day, config, timeZone, anchorTime);

        return allDayExecutions
            .Where(e => e > currentDateUtc)
            .Where(e => !config.LimitsEndDateLocal.HasValue || e <= config.LimitsEndDateLocal.Value)
            .Where(e => !config.LimitsStartDateLocal.HasValue || e >= config.LimitsStartDateLocal.Value)
            .OrderBy(e => e);
    }

    private static IEnumerable<DateTimeOffset> GenerateSingleExecution(DateOnly day, SchedulerConfiguration config, TimeZoneInfo timeZone, TimeSpan anchorTime)
    {
        var localDateTime = day.ToDateTime(TimeOnly.FromTimeSpan(anchorTime));

        // Comprobamos si la hora local es inválida por la transición de primavera (Spring Forward)
        if (timeZone.IsInvalidTime(localDateTime)) return Array.Empty<DateTimeOffset>();

        // Convertimos a UTC de forma segura
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);

        return new[] { new DateTimeOffset(utcDateTime, TimeSpan.Zero) };
    }

}