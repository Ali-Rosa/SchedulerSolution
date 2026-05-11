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
        return !string.IsNullOrWhiteSpace(locale) && ValidCultureNames.Contains(locale);
    }

    public static DayOfWeek GetFirstDayOfWeek(ScheduleConfiguration config)
    {
        // If the user sets a day, it will be used.
        if (config.FirstDayOfWeek.HasValue) return config.FirstDayOfWeek.Value;

        // Otherwise, we extract it from the culture (already validated beforehand).
        var culture = new CultureInfo(config.Locale);
        return culture.DateTimeFormat.FirstDayOfWeek;
    }

    public static CultureInfo GetCultureInfo(string locale)
    {
        // Safe method because it is called AFTER validation
        return new CultureInfo(locale);
    }
}