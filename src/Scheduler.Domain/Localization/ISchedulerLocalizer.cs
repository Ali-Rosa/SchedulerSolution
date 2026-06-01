using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Models.Weekly;

namespace Scheduler.Domain.Localization;

public interface ISchedulerLocalizer
{
    string Locale { get; }

    string FormatDate(DateTimeOffset dateTime);
    string FormatDate(DateOnly date);
    string FormatTime(TimeOnly time);
    string FormatTime(DateTimeOffset dateTime);

    string GetDayOfWeekName(DayOfWeek dayOfWeek);
    string GetOrdinalName(MonthlyRelativeOrdinal ordinal);
    string GetRelativeDayTypeName(MonthlyRelativeDayType dayType);
    string GetIntervalUnitName(TimeIntervalUnit unit, bool plural);

    string BuildOnceDescription(DateTimeOffset localTime, DateTimeOffset? limitsStartLocal, TimeZoneInfo timeZone);
    string BuildDailyPrefix(int recursEvery);
    string BuildWeeklyPrefix(IReadOnlyCollection<DayOfWeek> daysOfWeek, int recursEvery);
    string BuildMonthlyPrefix(SchedulerMonthly monthly, int recursEvery);

    string BuildFullDescription(string prefix, DateTimeOffset nextExecution, SchedulerConfiguration config, TimeZoneInfo timeZone);
    string GetValidationError(ValidationErrorKey key, params object[] args);
}