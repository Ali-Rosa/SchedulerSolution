using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class DailyFrecuencyRule
{
    public static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleDailyFrecuency schedule, TimeZoneInfo timeZone)
    {
        var start = day.ToDateTime(schedule.StartTime);
        var end = day.ToDateTime(schedule.EndTime);

        var current = start;

        while (current <= end)
        {
            yield return TimeZoneInfo.ConvertTimeToUtc(current, timeZone);

            current = schedule.IntervalUnit switch
            {
                TimeIntervalUnit.Hours => current.AddHours(schedule.FrequencyInterval),
                TimeIntervalUnit.Minutes => current.AddMinutes(schedule.FrequencyInterval),
                TimeIntervalUnit.Seconds => current.AddSeconds(schedule.FrequencyInterval),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
