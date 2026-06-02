using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using System.Globalization;

namespace Scheduler.Domain.Rules;

public static class CultureRule
{
    // We load all valid system cultures only once (Static cache)
    private static readonly HashSet<string> ValidCultureNames =
        CultureInfo.GetCultures(CultureTypes.AllCultures)
                   .Select(c => c.Name)
                   .Where(name => !string.IsNullOrEmpty(name))
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string locale)
    {
        return !string.IsNullOrWhiteSpace(locale) && 
            ValidCultureNames.Contains(locale) &&
            SchedulerLocalizerFactory.IsSupported(locale);
    }

    public static DayOfWeek GetFirstDayOfWeek(SchedulerConfiguration config)
    {
        if (config.FirstDayOfWeek.HasValue) return config.FirstDayOfWeek.Value;

        var culture = new CultureInfo(config.Locale!);
        return culture.DateTimeFormat.FirstDayOfWeek;
    }
}