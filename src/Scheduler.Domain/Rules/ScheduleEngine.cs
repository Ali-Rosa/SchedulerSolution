using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class ScheduleEngine
{
    public static SchedulerResponse IterateAndCalculate(
         SchedulerConfiguration config
        , TimeZoneInfo timeZone
        , Func<DateOnly, DateOnly, DateOnly?> getNextValidDayLogic
        , Func<DateTimeOffset, string> buildDescriptionLogic
    )
    {
        var results = new List<DateTimeOffset>();
        var currentLocal = TimeZoneInfo.ConvertTime(config.CurrentDate, timeZone);

        var seriesStartLocal = config.LimitsStartDateLocal.HasValue
            ? TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal.Value, timeZone)
            : currentLocal;

        var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);
        var anchorTime = seriesStartLocal.TimeOfDay;

        var searchCursorLocal = currentLocal > seriesStartLocal ? currentLocal : seriesStartLocal;
        var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);

        while (results.Count < config.MaxOccurrences)
        {
            var nextValidDay = getNextValidDayLogic(currentDay, seriesStartDay);

            if (!nextValidDay.HasValue) break;

            if (config.LimitsEndDateLocal.HasValue)
            {
                var limitEndLocal = TimeZoneInfo.ConvertTime(config.LimitsEndDateLocal.Value, timeZone);
                if (nextValidDay.Value > DateOnly.FromDateTime(limitEndLocal.DateTime)) break;
            }   

            var executions = GetExecutionsForDay(nextValidDay.Value, config.CurrentDate, config, timeZone, anchorTime);

            foreach (var execution in executions)
            {
                if (results.Count < config.MaxOccurrences) results.Add(execution);
            }

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

        // We check if the local time is invalid due to the Spring Forward transition.
        if (timeZone.IsInvalidTime(localDateTime)) return Array.Empty<DateTimeOffset>();

        // Convert to UTC safely
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);

        return new[] { new DateTimeOffset(utcDateTime, TimeSpan.Zero) };
    }

}