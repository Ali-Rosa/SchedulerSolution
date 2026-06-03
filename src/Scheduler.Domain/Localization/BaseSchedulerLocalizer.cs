using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using System.Globalization;

namespace Scheduler.Domain.Localization;

public abstract class BaseSchedulerLocalizer : ISchedulerLocalizer
{
    public abstract string Locale { get; }
    protected abstract CultureInfo Culture { get; }
    protected abstract string DateFormat { get; }
    protected abstract string TimeFormat { get; }

    public virtual string FormatDate(DateTimeOffset dateTime) => dateTime.ToString(DateFormat, Culture);
    public virtual string FormatDate(DateOnly date) => date.ToString(DateFormat, Culture);
    public virtual string FormatTime(TimeOnly time) => time.ToString(TimeFormat, Culture);
    public virtual string FormatTime(DateTimeOffset dateTime) => dateTime.ToString(TimeFormat, Culture);

    public abstract string GetDayOfWeekName(DayOfWeek dayOfWeek);
    public abstract string GetOrdinalName(MonthlyRelativeOrdinal ordinal);
    public abstract string GetRelativeDayTypeName(MonthlyRelativeDayType dayType);
    public abstract string GetIntervalUnitName(TimeIntervalUnit unit, bool plural);

    public abstract string BuildOnceDescription(DateTimeOffset localTime, DateTimeOffset? limitsStartLocal, TimeZoneInfo timeZone);
    public abstract string BuildDailyPrefix(int recursEvery);
    public abstract string BuildWeeklyPrefix(IReadOnlyCollection<DayOfWeek> daysOfWeek, int recursEvery);
    public abstract string BuildMonthlyPrefix(SchedulerMonthly monthly, int recursEvery);

    public abstract string BuildFullDescription(string prefix, DateTimeOffset nextExecution, SchedulerConfiguration config, TimeZoneInfo timeZone);
    public abstract string GetValidationError(ValidationErrorKey key, params object[] args);
}