using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class ScheduleEngine
{
    public static SchedulerResponse IterateAndCalculate (
        DateTimeOffset currentDateUtc
        , ScheduleConfiguration config
        , TimeZoneInfo timeZone
        , int maxOccurrences
        , Func<DateOnly, DateOnly, bool> isDayValidLogic
        , Func<DateTimeOffset, string> buildDescriptionLogic
    )
    {
        var results = new List<DateTimeOffset>();
        var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);
        var seriesStartLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);
        var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);
        var anchorTime = seriesStartLocal.TimeOfDay;

        var limitStartLocal = config.LimitsStartDateLocal.HasValue ? TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal.Value, timeZone) : seriesStartLocal;

        var searchCursorLocal = currentLocal > limitStartLocal ? currentLocal : limitStartLocal;

        for (int i = 0; i < 366 && results.Count < maxOccurrences; i++)
        {
            var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);

            if (isDayValidLogic(currentDay, seriesStartDay))
            {
                var executions = GetExecutionsForDay(currentDay, currentDateUtc, config, timeZone, anchorTime);

                foreach (var execution in executions)
                {
                    if (results.Count < maxOccurrences) results.Add(execution);
                }
            }

            if (config.LimitsEndDateLocal.HasValue && searchCursorLocal > config.LimitsEndDateLocal.Value)
                break;

            searchCursorLocal = new DateTimeOffset(currentDay.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone.GetUtcOffset(searchCursorLocal.DateTime));
        }

        if (!results.Any())
            return new SchedulerResponse("No valid executions found within the allowed range.");

        string description = buildDescriptionLogic(results.First());

        return new SchedulerResponse(results, description);
    }

    private static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone, TimeSpan anchorTime)
    {
        var allDayExecutions = (config.DailyFrequency != null) 
            ? DailyFrequencyRule.GetExecutionsForDay(day, config.DailyFrequency, timeZone) 
            : GenerateSingleExecution(day, config, timeZone, anchorTime);

        return allDayExecutions
            .Where(e => e > currentDateUtc)
            .Where(e => !config.LimitsEndDateLocal.HasValue || e <= config.LimitsEndDateLocal.Value)
            .Where(e => !config.LimitsStartDateLocal.HasValue || e >= config.LimitsStartDateLocal.Value)
            .OrderBy(e => e);
    }

    private static IEnumerable<DateTimeOffset> GenerateSingleExecution(DateOnly day, ScheduleConfiguration config, TimeZoneInfo timeZone, TimeSpan anchorTime)
    {
        TimeSpan time;

        time = anchorTime; // Use the anchor time for recurring schedules

        var localDateTime = day.ToDateTime(TimeOnly.FromTimeSpan(time));
        return new[] { new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime)) };
    }
}
