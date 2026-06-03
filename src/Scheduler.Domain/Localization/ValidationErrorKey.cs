namespace Scheduler.Domain.Localization;

public enum ValidationErrorKey
{
    ScheduleDisabled,
    UndefinedScheduleType,
    UndefinedOccursType,
    RecursEveryMustBePositive,
    StartDateLaterThanEndDate,
    TimeZoneRequired,
    LocaleRequired,
    InvalidFirstDayOfWeek,
    WeeklyConfigRequired,
    MonthlyConfigRequired,
    InvalidIntervalUnit,
    FrequencyIntervalMustBePositive,
    InvalidMonthlyDay,
    UndefinedRelativeOrdinal,
    UndefinedRelativeDayType,
    WeeklyConfigMinDays,
    ExecutionInPast,
    ExecutionBeforeLimits,
    ExecutionAfterLimits,
    NoExecutionsFound,
    ConfigNull,
    UnsupportedCombination,
    CultureNotSupported,
    InvalidTimeZone
}