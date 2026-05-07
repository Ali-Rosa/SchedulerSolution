using Scheduler.Domain.Models;

namespace Scheduler.Domain.Rules;

public static class DescriptionRule
{
    public static string Format(string prefix, DateTimeOffset nextExecution, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var desc = prefix;

        if (config.DailyFrecuency != null)
        {
            desc += $"Every {config.DailyFrecuency.FrequencyInterval} {config.DailyFrecuency.IntervalUnit.ToString().ToLower()} ";
        }

        desc += $"at {local:HH:mm}. Starting on {local:dd/MM/yyyy}";

        return desc;
    }
}