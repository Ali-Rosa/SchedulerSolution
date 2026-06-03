namespace Scheduler.Domain.Localization;

public static class SchedulerLocalizerFactory
{
    private static readonly Dictionary<string, ISchedulerLocalizer> _registry = new(StringComparer.OrdinalIgnoreCase)
    {
        { "es-ES", new SpanishLocalizer() },
        { "en-GB", new EnglishUkLocalizer() },
        { "en-US", new EnglishUsLocalizer() }
    };

    public static ISchedulerLocalizer GetLocalizer(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return _registry["en-US"];

        if (_registry.TryGetValue(locale, out var localizer))
            return localizer;

        if (locale.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return _registry["es-ES"];

        if (locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            if (locale.Contains("GB", StringComparison.OrdinalIgnoreCase) || locale.Contains("UK", StringComparison.OrdinalIgnoreCase))
                return _registry["en-GB"];
            return _registry["en-US"];
        }

        return _registry["en-US"];
    }

    public static bool IsSupported(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return false;

        return locale.StartsWith("es", StringComparison.OrdinalIgnoreCase) ||
               locale.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }
}