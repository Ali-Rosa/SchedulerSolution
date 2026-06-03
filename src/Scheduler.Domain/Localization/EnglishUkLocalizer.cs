using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using System.Globalization;

namespace Scheduler.Domain.Localization;

public class EnglishUkLocalizer : BaseSchedulerLocalizer
{
    public override string Locale => "en-GB";
    protected override CultureInfo Culture => new("en-GB");
    protected override string DateFormat => "dd/MM/yyyy";
    protected override string TimeFormat => "HH:mm";

    private static readonly Dictionary<ValidationErrorKey, string> ErrorMessages = new()
    {
        { ValidationErrorKey.ScheduleDisabled, "The schedule is disabled." },
        { ValidationErrorKey.UndefinedScheduleType, "Not defined schedule type." },
        { ValidationErrorKey.UndefinedOccursType, "Not defined occurs type." },
        { ValidationErrorKey.RecursEveryMustBePositive, "The Every value must be greater than 0." },
        { ValidationErrorKey.StartDateLaterThanEndDate, "Within the limits, the start date cannot be later than the end date." },
        { ValidationErrorKey.TimeZoneRequired, "The TimeZoneId is required." },
        { ValidationErrorKey.LocaleRequired, "The Locale is required." },
        { ValidationErrorKey.InvalidFirstDayOfWeek, "The provided FirstDayOfWeek is not a valid day of the week." },
        { ValidationErrorKey.WeeklyConfigRequired, "Weekly configuration is required for Weekly recurring schedules." },
        { ValidationErrorKey.MonthlyConfigRequired, "Monthly configuration is required for Monthly recurring schedules." },
        { ValidationErrorKey.InvalidIntervalUnit, "Not defined interval unit for daily frequency." },
        { ValidationErrorKey.FrequencyIntervalMustBePositive, "The frequency interval must be greater than 0." },
        { ValidationErrorKey.InvalidMonthlyDay, "The day must be between 1 and 31." },
        { ValidationErrorKey.UndefinedRelativeOrdinal, "Not defined relative ordinal: {0}." },
        { ValidationErrorKey.UndefinedRelativeDayType, "Not defined relative day type: {0}." },
        { ValidationErrorKey.WeeklyConfigMinDays, "Weekly configuration requires at least one day." },
        { ValidationErrorKey.ExecutionInPast, "The execution date cannot be in the past relative to the current date." },
        { ValidationErrorKey.ExecutionBeforeLimits, "The selected execution date is earlier than the allowed start limit date." },
        { ValidationErrorKey.ExecutionAfterLimits, "The selected execution date is later than the allowed end limit date." },
        { ValidationErrorKey.NoExecutionsFound, "No valid executions were found within the limits with this configuration." },
        { ValidationErrorKey.ConfigNull, "The configuration cannot be null." },
        { ValidationErrorKey.UnsupportedCombination, "Unsupported schedule and occurs combination." },
        { ValidationErrorKey.CultureNotSupported, "The culture '{0}' is not supported by the system." },
        { ValidationErrorKey.InvalidTimeZone, "Invalid TimeZoneId: {0}" }
    };

    public override string GetValidationError(ValidationErrorKey key, params object[] args)
    {
        if (ErrorMessages.TryGetValue(key, out var message))
        {
            return string.Format(message, args);
        }
        return key.ToString();
    }

    public override string GetDayOfWeekName(DayOfWeek dayOfWeek) => dayOfWeek.ToString().ToLower();

    public override string GetOrdinalName(MonthlyRelativeOrdinal ordinal) => ordinal switch
    {
        MonthlyRelativeOrdinal.First => "first",
        MonthlyRelativeOrdinal.Second => "second",
        MonthlyRelativeOrdinal.Third => "third",
        MonthlyRelativeOrdinal.Fourth => "fourth",
        MonthlyRelativeOrdinal.Last => "last",
        _ => ordinal.ToString().ToLower()
    };

    public override string GetRelativeDayTypeName(MonthlyRelativeDayType dayType) => dayType switch
    {
        MonthlyRelativeDayType.Day => "day",
        MonthlyRelativeDayType.Weekday => "weekday",
        MonthlyRelativeDayType.WeekendDay => "weekend day",
        _ => ((DayOfWeek)dayType).ToString().ToLower()
    };

    public override string GetIntervalUnitName(TimeIntervalUnit unit, bool plural) => unit switch
    {
        TimeIntervalUnit.Hours => plural ? "hours" : "hour",
        TimeIntervalUnit.Minutes => plural ? "minutes" : "minute",
        TimeIntervalUnit.Seconds => plural ? "seconds" : "second",
        _ => unit.ToString().ToLower()
    };

    public override string BuildOnceDescription(DateTimeOffset localTime, DateTimeOffset? limitsStartLocal, TimeZoneInfo timeZone)
    {
        var desc = $"Occurs once. Schedule will be used on {FormatDate(localTime)} at {FormatTime(localTime)} ";
        if (limitsStartLocal.HasValue)
        {
            var startLocal = TimeZoneInfo.ConvertTime(limitsStartLocal.Value, timeZone);
            desc += $"starting on {FormatDate(startLocal)}";
        }
        return desc;
    }

    public override string BuildDailyPrefix(int recursEvery)
    {
        return recursEvery == 1 ? "Occurs every day. " : $"Occurs every {recursEvery} days. ";
    }

    public override string BuildWeeklyPrefix(IReadOnlyCollection<DayOfWeek> daysOfWeek, int recursEvery)
    {
        var daysText = JoinDays(daysOfWeek);
        var weekText = recursEvery == 1 ? "week" : $"{recursEvery} weeks";
        return $"Occurs every {weekText} on {daysText}. ";
    }

    public override string BuildMonthlyPrefix(SchedulerMonthly monthly, int recursEvery)
    {
        var monthText = recursEvery == 1 ? "every month. " : $"every {recursEvery} months. ";
        if (monthly.IsSpecificDay)
        {
            return $"Occurs day {monthly.SpecificDayNumber} of {monthText}";
        }

        var ordinal = GetOrdinalName(monthly.RelativeOrdinal!.Value);
        var dayType = GetRelativeDayTypeName(monthly.RelativeDayType!.Value);
        return $"Occurs the {ordinal} {dayType} of {monthText}";
    }

    public override string BuildFullDescription(string prefix, DateTimeOffset nextExecution, SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var desc = prefix;

        if (config.DailyFrequencyConfiguration != null && config.DailyFrequencyConfiguration.OccursEveryEnable)
        {
            var unit = GetIntervalUnitName(config.DailyFrequencyConfiguration.IntervalUnit, config.DailyFrequencyConfiguration.FrequencyInterval > 1);
            desc += $"Every {config.DailyFrequencyConfiguration.FrequencyInterval} {unit} ";
        }

        desc += $"at {FormatTime(local)}. Starting on {FormatDate(local)}";
        return desc;
    }

    private string JoinDays(IReadOnlyCollection<DayOfWeek> days)
    {
        var names = days.Select(GetDayOfWeekName).ToList();
        if (names.Count == 0) return string.Empty;
        if (names.Count == 1) return names[0];

        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names.Last();
    }
}