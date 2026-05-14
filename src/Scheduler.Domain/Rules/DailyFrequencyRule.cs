using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class DailyFrequencyRule
{
    public static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleDailyFrequency schedule, TimeZoneInfo timeZone)
    {
        // Single execution in the day (OccursOnceEnable)
        if (schedule.OccursOnceEnable)
        {
            var dt = day.ToDateTime(schedule.OnceTime);
            if (!timeZone.IsInvalidTime(dt))
            {
                yield return TimeZoneInfo.ConvertTimeToUtc(dt, timeZone);
            }
            yield break; // End here if it's a single execution
        }

        // Recurrent intra-day execution (OccursEveryEnable)
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
                    SchedulerTimeIntervalUnit.Hours => current.AddHours(schedule.FrequencyInterval),
                    SchedulerTimeIntervalUnit.Minutes => current.AddMinutes(schedule.FrequencyInterval),
                    SchedulerTimeIntervalUnit.Seconds => current.AddSeconds(schedule.FrequencyInterval),
                    _ => end.AddTicks(1)
                };
            }
        }
    }
}