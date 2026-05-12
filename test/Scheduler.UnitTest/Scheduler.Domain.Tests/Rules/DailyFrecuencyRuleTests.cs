using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class DailyFrecuencyRuleTests
{
    [Fact]
    public void Should_Return_Empty_If_No_Execution_Mode_Is_Enabled()
    {
        // Arrange
        var day = new DateOnly(2026, 5, 1);
        var schedule = new ScheduleDailyFrecuency(false, default, false, default, 0, default, default);

        // Act
        var result = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Hourly_Intervals_Should_Be_Generated_Correctly_Within_Range()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: false,
            OnceTime: default,
            OccursEveryEnable: true,
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 2,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(8, 0)
        );

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        // Assert
        executions.Count.ShouldBe(3);
        executions[0].ShouldBe(new DateTimeOffset(2020, 1, 1, 4, 0, 0, TimeSpan.Zero));
        executions[1].ShouldBe(new DateTimeOffset(2020, 1, 1, 6, 0, 0, TimeSpan.Zero));
        executions[2].ShouldBe(new DateTimeOffset(2020, 1, 1, 8, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Occurs_Once_Mode_Should_Generate_Exactly_One_Execution()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);
        var onceTime = new TimeOnly(14, 30);
        var schedule = new ScheduleDailyFrecuency(
            OccursOnceEnable: true,
            OnceTime: onceTime,
            OccursEveryEnable: false, // Ignored
            IntervalUnit: default, FrequencyInterval: 0, StartTime: default, EndTime: default
        );

        // Act
        var result = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result.First().Hour.ShouldBe(14);
        result.First().Minute.ShouldBe(30);
    }

    [Fact]
    public void Intervals_Should_Not_Be_Generated_Beyond_End_Time()
    {
        // Arrange: 4 AM to 8 AM every 3 hours -> 4 AM, 7 AM. (Leaving at 10 AM)
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Hours, 3, new TimeOnly(4, 0), new TimeOnly(8, 0));

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc).ToList();

        // Assert
        executions.Count.ShouldBe(2);
        executions.Last().Hour.ShouldBe(7);
    }

    [Fact]
    public void Local_Times_Should_Be_Correctly_Converted_To_Utc()
    {
        // Arrange: 4:00 AM in Central European Time (CET)
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(4, 0));
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        // Act
        var execution = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, timeZone).Single();

        // Assert: 04:00 CET = 03:00 UTC in winter
        execution.UtcDateTime.Hour.ShouldBe(3);
        execution.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Minute_And_Second_Intervals_Should_Be_Generated_Correctly()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);
        var scheduleMin = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Minutes, 15, new TimeOnly(8, 0), new TimeOnly(8, 30));
        var scheduleSec = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Seconds, 30, new TimeOnly(12, 0, 0), new TimeOnly(12, 0, 30));

        // Act
        var resMin = DailyFrecuencyRule.GetExecutionsForDay(day, scheduleMin, TimeZoneInfo.Utc).ToList();
        var resSec = DailyFrecuencyRule.GetExecutionsForDay(day, scheduleSec, TimeZoneInfo.Utc).ToList();

        // Assert
        resMin.Count.ShouldBe(3); // 8:00, 8:15, 8:30
        resSec.Count.ShouldBe(2); // 12:00:00, 12:00:30
        resSec.Last().Second.ShouldBe(30);
    }

    [Fact]
    public void Invalid_Time_Ranges_Should_Return_Empty_List()
    {
        // Arrange: Start time after end time
        var day = new DateOnly(2020, 1, 1);
        var schedule = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Hours, 1, new TimeOnly(22, 0), new TimeOnly(8, 0));

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, TimeZoneInfo.Utc);

        // Assert
        executions.ShouldBeEmpty();
    }

    [Fact]
    public void Daylight_Saving_Time_Spring_Forward_Gap_Should_Be_Skipped()
    {
        // Arrange: March 31, 2024 in Europe (spring forward from 02:00 to 03:00)
        var day = new DateOnly(2024, 3, 31);
        var schedule = new ScheduleDailyFrecuency(false, default, true, TimeIntervalUnit.Hours, 1, new TimeOnly(1, 30), new TimeOnly(3, 30));
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

        // Act
        var executions = DailyFrecuencyRule.GetExecutionsForDay(day, schedule, timeZone).ToList();

        // Assert: 1:30 (ok), 2:30 (invalid, skipped), 3:30 (ok)
        executions.Count.ShouldBe(2);
        executions.First().Hour.ShouldBe(0); // 01:30 local = 00:30 UTC
        executions.Last().Hour.ShouldBe(1);  // 03:30 local = 01:30 UTC
    }

}