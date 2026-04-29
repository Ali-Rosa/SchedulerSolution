using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class IntraDayRule
{
    public static IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, IntraDaySchedule schedule, TimeZoneInfo timeZone)
    {
        var start = day.ToDateTime(schedule.StartTime);
        var end = day.ToDateTime(schedule.EndTime);

        var current = start;

        while (current <= end)
        {
            yield return TimeZoneInfo.ConvertTimeToUtc(current, timeZone);

            current = schedule.Unit switch
            {
                IntraDayFrequencyUnit.Hours => current.AddHours(schedule.Every),
                IntraDayFrequencyUnit.Minutes => current.AddMinutes(schedule.Every),
                IntraDayFrequencyUnit.Seconds => current.AddSeconds(schedule.Every),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
