using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Tests.Rules;

public class DailyFrecuencyRuleTests
{
    [Fact]
    public void Generates_executions_every_2_hours_within_range()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);

        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: false,
            OnceTime: new TimeOnly(0, 0),
            OccursEveryEnable: true,
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 2,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(8, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        // Act
        var executions = DailyFrecuencyRule .GetExecutionsForDay(day, schedule, timeZone) .ToList();

        // Assert
        Assert.Equal(3, executions.Count);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 4, 0, 0, TimeSpan.Zero), executions[0]);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 6, 0, 0, TimeSpan.Zero), executions[1]);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 8, 0, 0, TimeSpan.Zero), executions[2]);
    }

    [Fact]
    public void Generates_single_execution_when_start_equals_end()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: false,
            OnceTime: new TimeOnly(0, 0),
            OccursEveryEnable: true,
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 1,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(10, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        var executions = DailyFrecuencyRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .ToList();

        Assert.Single(executions);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero), executions[0]);
    }

    [Fact]
    public void Does_not_generate_execution_after_end_time()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: false,
            OnceTime: new TimeOnly(0, 0),
            OccursEveryEnable: true,
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 3,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(8, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        var executions = DailyFrecuencyRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .ToList();

        Assert.Equal(2, executions.Count);
        Assert.Equal(new TimeOnly(4, 0), TimeOnly.FromDateTime(executions[0].UtcDateTime));
        Assert.Equal(new TimeOnly(7, 0), TimeOnly.FromDateTime(executions[1].UtcDateTime));
    }

    [Fact]
    public void Converts_local_times_to_utc_correctly()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: false,
            OnceTime: new TimeOnly(0, 0),
            OccursEveryEnable: true,
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 2,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(4, 0)
        );

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        var execution = DailyFrecuencyRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .Single();

        // 04:00 CET = 03:00 UTC in winter
        Assert.Equal(new TimeOnly(3, 0), TimeOnly.FromDateTime(execution.UtcDateTime));
    }

    [Fact]
    public void Generates_executions_every_15_minutes()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(
            false, default, true,
            TimeIntervalUnit.Minutes, 15, // Every 15 minutes
            new TimeOnly(8, 0), new TimeOnly(9, 0)
        );

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        // Assert: 8:00, 8:15, 8:30, 8:45, 9:00 -> 5 executions
        Assert.Equal(5, executions.Count);
        Assert.Equal(new TimeOnly(8, 45), TimeOnly.FromDateTime(executions[3].UtcDateTime));
    }

    [Fact]
    public void Generates_executions_every_30_seconds()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(
            false
            , default, true,
            TimeIntervalUnit.Seconds, 30,
            new TimeOnly(12, 0, 0), new TimeOnly(12, 1, 0) // One minute of range
        );

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        // Assert: 12:00:00, 12:00:30, 12:01:00 -> 3 executions
        Assert.Equal(3, executions.Count);
        Assert.Equal(0, executions[2].Second); // Exactly second 0 of the next minute
    }

    [Fact]
    public void Returns_empty_list_when_start_time_is_after_end_time()
    {
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(
            false, default, true,
            TimeIntervalUnit.Hours, 1,
            new TimeOnly(22, 0), new TimeOnly(8, 0)
        );

        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        Assert.Empty(executions);
    }

    [Fact]
    public void Handles_Daylight_Saving_Time_Spring_Forward()
    {
        // March 31, 2024: The clock jumps from 02:00 to 03:00 in Europe
        var day = new DateOnly(2024, 3, 31);
        var schedule = new ScheduleDailyFrecuency(
            false
            , default
            , true
            , TimeIntervalUnit.Hours
            , 1
            , new TimeOnly(1, 30), new TimeOnly(3, 30)
        );

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, timeZone).ToList();

        // Assert:
        // 01:30 local time -> EXISTS (00:30 UTC)
        // 02:30 local time -> DOES NOT EXIST (Should be skipped)
        // 03:30 local time -> EXISTS (01:30 UTC)

        Assert.Equal(2, executions.Count);
        Assert.Equal(new TimeOnly(0, 30), TimeOnly.FromDateTime(executions[0].UtcDateTime));
        Assert.Equal(new TimeOnly(1, 30), TimeOnly.FromDateTime(executions[1].UtcDateTime));
    }

}