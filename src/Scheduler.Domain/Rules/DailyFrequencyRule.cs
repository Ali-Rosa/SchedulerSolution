using Scheduler.Domain.Models.Daily;

namespace Scheduler.Domain.Rules;

public static class DailyFrequencyRule
{
    public static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleDailyFrequency schedule, TimeZoneInfo timeZone)
    {
        if (schedule.OccursEveryEnable)
        {
            var start = day.ToDateTime(schedule.StartTime);
            var end = day.ToDateTime(schedule.EndTime);
            var current = start;

            while (current <= end)
            {
                if (!timeZone.IsInvalidTime(current))
                {
                    yield return TimeZoneInfo.ConvertTimeToUtc(current, timeZone);
                }

                current = schedule.IntervalUnit switch
                {
                    TimeIntervalUnit.Hours => current.AddHours(schedule.FrequencyInterval),
                    TimeIntervalUnit.Minutes => current.AddMinutes(schedule.FrequencyInterval),
                    TimeIntervalUnit.Seconds => current.AddSeconds(schedule.FrequencyInterval),
                    _ => end.AddTicks(1)
                };
            }
        }
        else
        {
            var dt = day.ToDateTime(schedule.OnceTime);
            if (!timeZone.IsInvalidTime(dt))
            {
                yield return TimeZoneInfo.ConvertTimeToUtc(dt, timeZone);
            }
            yield break;
        }
    }
}