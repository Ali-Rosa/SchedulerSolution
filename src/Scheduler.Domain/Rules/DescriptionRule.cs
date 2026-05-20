using Scheduler.Domain.Models;
using System.Globalization;

namespace Scheduler.Domain.Rules;

public static class DescriptionRule
{
    public static string BuildExecutionDescription(string prefix, DateTimeOffset nextExecution, SchedulerConfiguration config, TimeZoneInfo timeZone, CultureInfo culture)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var desc = prefix;

        if (config.DailyFrequencyConfiguration != null)
        {
            desc += $"Every {config.DailyFrequencyConfiguration.FrequencyInterval} {config.DailyFrequencyConfiguration.IntervalUnit.ToString().ToLower()} ";
        }

        desc += $"at {local:HH:mm}. Starting on {local:dd/MM/yyyy}";

        return desc;
    }

}