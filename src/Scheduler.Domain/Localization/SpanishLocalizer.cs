using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using System.Globalization;

namespace Scheduler.Domain.Localization;

public class SpanishLocalizer : BaseSchedulerLocalizer
{
    public override string Locale => "es-ES";
    protected override CultureInfo Culture => new("es-ES");
    protected override string DateFormat => "dd/MM/yyyy";
    protected override string TimeFormat => "HH:mm";

    private static readonly Dictionary<ValidationErrorKey, string> ErrorMessages = new()
    {
        { ValidationErrorKey.ScheduleDisabled, "La planificación está deshabilitada." },
        { ValidationErrorKey.UndefinedScheduleType, "Tipo de planificación no definido." },
        { ValidationErrorKey.UndefinedOccursType, "Tipo de ocurrencia no definido." },
        { ValidationErrorKey.RecursEveryMustBePositive, "El valor 'Cada' debe ser mayor que 0." },
        { ValidationErrorKey.StartDateLaterThanEndDate, "Dentro de los límites, la fecha de inicio no puede ser posterior a la de fin." },
        { ValidationErrorKey.TimeZoneRequired, "El TimeZoneId es requerido." },
        { ValidationErrorKey.LocaleRequired, "El Locale es requerido." },
        { ValidationErrorKey.InvalidFirstDayOfWeek, "El FirstDayOfWeek proporcionado no es un día válido de la semana." },
        { ValidationErrorKey.WeeklyConfigRequired, "La configuración semanal es requerida para planificaciones recurrentes semanales." },
        { ValidationErrorKey.MonthlyConfigRequired, "La configuración mensual es requerida para planificaciones recurrentes mensuales." },
        { ValidationErrorKey.InvalidIntervalUnit, "Unidad de intervalo no definida para la frecuencia diaria." },
        { ValidationErrorKey.FrequencyIntervalMustBePositive, "El intervalo de frecuencia debe ser mayor que 0." },
        { ValidationErrorKey.InvalidMonthlyDay, "El día debe estar entre 1 y 31." },
        { ValidationErrorKey.UndefinedRelativeOrdinal, "Ordinal relativo no definido: {0}." },
        { ValidationErrorKey.UndefinedRelativeDayType, "Tipo de día relativo no definido: {0}." },
        { ValidationErrorKey.WeeklyConfigMinDays, "La configuración semanal requiere al menos un día." },
        { ValidationErrorKey.ExecutionInPast, "La fecha de ejecución no puede estar en el pasado con respecto a la fecha actual." },
        { ValidationErrorKey.ExecutionBeforeLimits, "La fecha de ejecución seleccionada es anterior a la fecha límite de inicio permitida." },
        { ValidationErrorKey.ExecutionAfterLimits, "La fecha de ejecución seleccionada es posterior a la fecha límite de fin permitida." },
        { ValidationErrorKey.NoExecutionsFound, "No se encontraron ejecuciones válidas dentro de los límites con esta configuración." },
        { ValidationErrorKey.ConfigNull, "La configuración no puede ser nula." },
        { ValidationErrorKey.UnsupportedCombination, "Combinación de planificación y ocurrencia no soportada." },
        { ValidationErrorKey.CultureNotSupported, "La cultura '{0}' no está soportada por el sistema." },
        { ValidationErrorKey.InvalidTimeZone, "TimeZoneId inválido: {0}" }
    };

    public override string GetValidationError(ValidationErrorKey key, params object[] args)
    {
        if (ErrorMessages.TryGetValue(key, out var message))
        {
            return string.Format(message, args);
        }
        return key.ToString();
    }

    public override string GetDayOfWeekName(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Sunday => "domingo",
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        _ => dayOfWeek.ToString().ToLower()
    };

    public override string GetOrdinalName(MonthlyRelativeOrdinal ordinal) => ordinal switch
    {
        MonthlyRelativeOrdinal.First => "primer",
        MonthlyRelativeOrdinal.Second => "segundo",
        MonthlyRelativeOrdinal.Third => "tercer",
        MonthlyRelativeOrdinal.Fourth => "cuarto",
        MonthlyRelativeOrdinal.Last => "último",
        _ => ordinal.ToString().ToLower()
    };

    public override string GetRelativeDayTypeName(MonthlyRelativeDayType dayType) => dayType switch
    {
        MonthlyRelativeDayType.Day => "día",
        MonthlyRelativeDayType.Weekday => "día de la semana",
        MonthlyRelativeDayType.WeekendDay => "día de fin de semana",
        _ => GetDayOfWeekName((DayOfWeek)dayType)
    };

    public override string GetIntervalUnitName(TimeIntervalUnit unit, bool plural) => unit switch
    {
        TimeIntervalUnit.Hours => plural ? "horas" : "hora",
        TimeIntervalUnit.Minutes => plural ? "minutos" : "minuto",
        TimeIntervalUnit.Seconds => plural ? "segundos" : "segundo",
        _ => unit.ToString().ToLower()
    };

    public override string BuildOnceDescription(DateTimeOffset localTime, DateTimeOffset? limitsStartLocal, TimeZoneInfo timeZone)
    {
        var desc = $"Ocurre una vez. La planificación se usará el {FormatDate(localTime)} a las {FormatTime(localTime)} ";
        if (limitsStartLocal.HasValue)
        {
            var startLocal = TimeZoneInfo.ConvertTime(limitsStartLocal.Value, timeZone);
            desc += $"comenzando el {FormatDate(startLocal)}";
        }
        return desc;
    }

    public override string BuildDailyPrefix(int recursEvery)
    {
        return recursEvery == 1 ? "Ocurre cada día. " : $"Ocurre cada {recursEvery} días. ";
    }

    public override string BuildWeeklyPrefix(IReadOnlyCollection<DayOfWeek> daysOfWeek, int recursEvery)
    {
        var daysText = JoinDays(daysOfWeek);
        var weekText = recursEvery == 1 ? "semana" : $"{recursEvery} semanas";
        return $"Ocurre cada {weekText} el {daysText}. ";
    }

    public override string BuildMonthlyPrefix(SchedulerMonthly monthly, int recursEvery)
    {
        var monthText = recursEvery == 1 ? "mes. " : $"{recursEvery} meses. ";
        if (monthly.IsSpecificDay)
        {
            return $"Ocurre el día {monthly.SpecificDayNumber} de cada {monthText}";
        }

        var ordinal = GetOrdinalName(monthly.RelativeOrdinal!.Value);
        var dayType = GetRelativeDayTypeName(monthly.RelativeDayType!.Value);
        return $"Ocurre el {ordinal} {dayType} de cada {monthText}";
    }

    public override string BuildFullDescription(string prefix, DateTimeOffset nextExecution, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var desc = prefix;

        if (config.DailyFrequencyConfiguration != null && config.DailyFrequencyConfiguration.OccursEveryEnable)
        {
            var unit = GetIntervalUnitName(config.DailyFrequencyConfiguration.IntervalUnit, config.DailyFrequencyConfiguration.FrequencyInterval > 1);
            desc += $"Cada {config.DailyFrequencyConfiguration.FrequencyInterval} {unit} ";
        }

        desc += $"a las {FormatTime(local)}. Comenzando el {FormatDate(local)}";
        return desc;
    }

    private string JoinDays(IReadOnlyCollection<DayOfWeek> days)
    {
        var names = days.Select(GetDayOfWeekName).ToList();
        if (names.Count == 0) return string.Empty;
        if (names.Count == 1) return names[0];

        return string.Join(", ", names.Take(names.Count - 1)) + " y " + names.Last();
    }
}