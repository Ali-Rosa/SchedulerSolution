using Scheduler.Domain.Models;

namespace Scheduler.Domain.Tests.TestHelpers.Builders;

public sealed class ScheduleConfigurationBuilder
{
    private bool _enabled = true;
    private ScheduleType _type = ScheduleType.Once;
    private OccursType _occurs = OccursType.Daily;
    private int _every = 1;
    private string _timeZoneId = TimeZoneInfo.Utc.Id;
    private DateTimeOffset? _execution;
    private DateTimeOffset? _start;
    private DateTimeOffset? _end;
    private ScheduleIntraDay? _intraDay;
    private ScheduleWeekly? _weekly;


    public static ScheduleConfigurationBuilder OnceDaily()
        => new() { _type = ScheduleType.Once, _every = 0 };

    public static ScheduleConfigurationBuilder RecurringDaily()
        => new() { _type = ScheduleType.Recurring, _every = 1 };

    public static ScheduleConfigurationBuilder RecurringWeekly()
        => new() { _type = ScheduleType.Recurring, _every = 1 };

    public ScheduleConfigurationBuilder Disabled()
    {
        _enabled = false;
        return this;
    }

    public ScheduleConfigurationBuilder WithInvalidScheduleType()
    {
        _type = (ScheduleType)1222;
        return this;
    }

    public ScheduleConfigurationBuilder WithInvalidOccursType()
    {
        _occurs = (OccursType)1999;
        return this;
    }

    public ScheduleConfigurationBuilder WithEvery(int value)
    {
        _every = value;
        return this;
    }

    public ScheduleConfigurationBuilder WithTimeZone(string timeZoneId)
    {
        _timeZoneId = timeZoneId;
        return this;
    }

    public ScheduleConfigurationBuilder WithExecution(DateTimeOffset value)
    {
        _execution = value;
        return this;
    }

    public ScheduleConfigurationBuilder WithStartDate(DateTimeOffset value)
    {
        _start = value;
        return this;
    }

    public ScheduleConfigurationBuilder WithEndDate(DateTimeOffset value)
    {
        _end = value;
        return this;
    }

    public ScheduleConfigurationBuilder WithOccurs(OccursType occurs)
    {
        _occurs = occurs;
        return this;
    }

    public ScheduleConfigurationBuilder WithIntraDay(bool ocursOnceEnable, TimeOnly onceTime, bool ocursEveryEnable, IntraDayFrequencyUnit unit, int every, TimeOnly start, TimeOnly end)
    {
        _intraDay = new ScheduleIntraDay(ocursOnceEnable, onceTime, ocursEveryEnable, unit, every, start, end);
        return this;
    }

    public ScheduleConfigurationBuilder WithIntraDayOnce(TimeOnly onceTime)
    {
        _intraDay = new ScheduleIntraDay(true, onceTime, false, default, 0, default, default);
        return this;
    }

    public ScheduleConfigurationBuilder WithIntraDayEvery(IntraDayFrequencyUnit unit, int every, TimeOnly start, TimeOnly end)
    {
        _intraDay = new ScheduleIntraDay(false, default, true, unit, every, start, end);
        return this;
    }



    public ScheduleConfigurationBuilder WithWeekly(int everyWeeks, params DayOfWeek[] days)
    {
        _weekly = new ScheduleWeekly(everyWeeks, days);
        _occurs = OccursType.Weekly;
        return this;
    }


    /// <summary>
    /// </summary>

    public ScheduleConfiguration Build()
    {
        return new ScheduleConfiguration(
            _enabled,
            _type,
            _execution,
            _occurs,
            _every,
            _start,
            _end,
            _timeZoneId,
            _intraDay,
            _weekly
        );
    }
}