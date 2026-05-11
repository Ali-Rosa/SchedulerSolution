using Scheduler.Domain.Models;
using System.Globalization;

namespace Scheduler.Domain.Rules;

public static class DescriptionRule
{
    public static string FormatMenssajeDescrictionResponse(string prefix, DateTimeOffset nextExecution, ScheduleConfiguration config, TimeZoneInfo timeZone, CultureInfo culture)
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