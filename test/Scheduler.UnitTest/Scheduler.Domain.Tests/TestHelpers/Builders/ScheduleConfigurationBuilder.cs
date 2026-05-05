using Scheduler.Domain.Models;

namespace Scheduler.Domain.Tests.TestHelpers.Builders;

public sealed class ScheduleConfigurationBuilder
{
    private bool _enabled = true;
    private ScheduleType _type = ScheduleType.Once;
    private OccursType _occurs = OccursType.Daily;
    private int _recursEvery = 1;
    private string _timeZoneId = TimeZoneInfo.Utc.Id;
    private DateTimeOffset? _executionDateTimeLocal;
    private DateTimeOffset? _limitsStartDateLocal;
    private DateTimeOffset? _limitsEndDateLocal;
    private ScheduleDailyFrecuency? _dailyFrecuency;
    private ScheduleWeekly? _weekly;

    public static ScheduleConfigurationBuilder OnceDaily() => new() { _type = ScheduleType.Once, _recursEvery = 0 };

    public static ScheduleConfigurationBuilder RecurringDaily() => new() { _type = ScheduleType.Recurring, _recursEvery = 1 };

    public static ScheduleConfigurationBuilder RecurringWeekly() => new() { _type = ScheduleType.Recurring, _recursEvery = 1 };

    public ScheduleConfigurationBuilder Disabled()
    {
        _enabled = false;
        return this;
    }

    public ScheduleConfigurationBuilder With_Invalid_ScheduleType()
    {
        _type = (ScheduleType)1222;
        return this;
    }

    public ScheduleConfigurationBuilder With_Invalid_OccursType()
    {
        _occurs = (OccursType)1999;
        return this;
    }

    public ScheduleConfigurationBuilder With_RecursEvery(int recursEvery)
    {
        _recursEvery = recursEvery;
        return this;
    }

    public ScheduleConfigurationBuilder With_TimeZoneId(string timeZoneId)
    {
        _timeZoneId = timeZoneId;
        return this;
    }

    public ScheduleConfigurationBuilder With_ExecutionDateTimeLocal(DateTimeOffset executionDateTimeLocal)
    {
        _executionDateTimeLocal = executionDateTimeLocal;
        return this;
    }

    public ScheduleConfigurationBuilder With_Limits_StartDateLocal(DateTimeOffset limitsStartDateLocal)
    {
        _limitsStartDateLocal = limitsStartDateLocal;
        return this;
    }

    public ScheduleConfigurationBuilder With_Limits_EndDateLocal(DateTimeOffset limitsEndDateLocal)
    {
        _limitsEndDateLocal = limitsEndDateLocal;
        return this;
    }

    public ScheduleConfigurationBuilder With_Occurs(OccursType occurs)
    {
        _occurs = occurs;
        return this;
    }

    public ScheduleConfigurationBuilder With_DailyFrecuency(bool ocursOnceEnable, TimeOnly onceTime, bool ocursEveryEnable, TimeIntervalUnit intervalUnit, int frequencyInterval, TimeOnly startTime, TimeOnly endTime)
    {
        _dailyFrecuency = new ScheduleDailyFrecuency(ocursOnceEnable, onceTime, ocursEveryEnable, intervalUnit, frequencyInterval, startTime, endTime);
        return this;
    }

    public ScheduleConfigurationBuilder With_DailyFrecuency_OccursOnce(TimeOnly onceTime)
    {
        _dailyFrecuency = new ScheduleDailyFrecuency(true, onceTime, false, default, 0, default, default);
        return this;
    }

    public ScheduleConfigurationBuilder With_DailyFrecuency_OccursEvery(TimeIntervalUnit intervalUnit, int frequencyInterval, TimeOnly startTime, TimeOnly endTime)
    {
        _dailyFrecuency = new ScheduleDailyFrecuency(false, default, true, intervalUnit, frequencyInterval, startTime, endTime);
        return this;
    }



    public ScheduleConfigurationBuilder WithWeekly(int everyWeeks, params DayOfWeek[] days)
    {
        _weekly = new ScheduleWeekly(everyWeeks, days);
        _occurs = OccursType.Weekly;
        return this;
    }


    /// <summary>
    /// Builds the ScheduleConfiguration instance.
    /// </summary>

    public ScheduleConfiguration Build()
    {
        return new ScheduleConfiguration(
            _enabled,
            _type,
            _executionDateTimeLocal,
            _occurs,
            _recursEvery,
            _limitsStartDateLocal,
            _limitsEndDateLocal,
            _timeZoneId,
            _dailyFrecuency,
            _weekly
        );
    }
}