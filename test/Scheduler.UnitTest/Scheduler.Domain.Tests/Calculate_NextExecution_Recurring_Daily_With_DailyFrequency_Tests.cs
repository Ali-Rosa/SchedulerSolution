using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class Calculate_NextExecution_Recurring_Daily_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service = new([new RecurringDailySchedulerStrategy()]);

    #region Precedence & Exclusivity Logic

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Mode_OccursOnce_Should_Take_Precedence_Over_OccursEvery_YA_NO_EXISTE()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    #endregion Precedence & Exclusivity Logic

    #region Mode: Occurs Once

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_OccursOnce_Should_Return_Today_If_Time_Is_In_The_Future()
    {
        // Arrange: 10 AM now, execution at 3 PM today.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_OccursOnce_Should_Jump_To_Next_Valid_Day_If_Time_Already_Passed()
    {
        // Arrange: 10 PM now, execution was at 8 AM. Should be tomorrow.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(8, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(7);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
    }

    #endregion Mode: Occurs Once

    #region Mode: Occurs Every (Intervals)

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_OccursEvery_Should_Find_Next_Interval_Within_The_Same_Day()
    {
        // Arrange: 5 AM now. Hours: 4, 6, 8 AM. Next: 6 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_OccursEvery_Should_Jump_To_Next_Pattern_Day_If_Day_Range_Is_Exhausted()
    {
        // Arrange: 10 PM now. Pattern every 3 days. Hours 4-8 AM. Next: Day 09 at 4 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 3,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(9);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
    }

    [Theory]
    [InlineData(TimeIntervalUnit.Minutes, 15, 12, 15)]
    [InlineData(TimeIntervalUnit.Seconds, 20, 12, 0, 20)]
    public void Calculate_NextExecution_Recurring_Daily_OccursEvery_Should_Handle_Small_Time_Units_Correctly(TimeIntervalUnit unit, int interval, int h, int m, int s = 0)
    {
        // Arrange: 12:00:00 exact.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = unit,
                FrequencyInterval = interval,
                StartTime = new TimeOnly(12, 0, 0),
                EndTime = new TimeOnly(13, 0, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(h);
        result.NextExecutionTime.Value.Minute.ShouldBe(m);
        result.NextExecutionTime.Value.Second.ShouldBe(s);
    }

    #endregion Mode: Occurs Every (Intervals)

    #region Edge Cases & Transitions

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Start_Time_Exactly_Now_Should_Find_The_Next_Available_Interval()
    {
        // Scenario: If it's exactly 04:00:00, the filter 'e > now' should jump to 06:00.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 4, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
    }

    #endregion Edge Cases & Transitions

    #region Localization & Description Verification

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Description_Should_Include_Detailed_Frequency_Information()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.Description.ShouldContain("Every 2 hours");
        result.Description.ShouldContain("at 04:00");
    }

    #endregion Localization & Description Verification







}