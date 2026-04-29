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

    public static ScheduleConfigurationBuilder Once()
        => new() { _type = ScheduleType.Once, _every = 0 };

    public static ScheduleConfigurationBuilder Recurring()
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
            null,
            null
        );
    }
}