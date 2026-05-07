using System.Globalization;

namespace Scheduler.Domain.Rules;

public static class CultureRule
{
    // Cargamos todas las culturas válidas del sistema una sola vez (Caché estática)
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
        // Si el usuario forzó un día, usamos ese.
        if (config.FirstDayOfWeek.HasValue) return config.FirstDayOfWeek.Value;

        // Si no, lo extraemos de la cultura (ya validada previamente)
        var culture = new CultureInfo(config.Locale);
        return culture.DateTimeFormat.FirstDayOfWeek;
    }

    public static CultureInfo GetCultureInfo(string locale)
    {
        // Método seguro porque se llama DESPUÉS de validar
        return new CultureInfo(locale);
    }
}