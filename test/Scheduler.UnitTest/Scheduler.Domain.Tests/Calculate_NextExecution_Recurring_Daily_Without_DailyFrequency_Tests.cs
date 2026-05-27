using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service = new([new RecurringDailySchedulerStrategy()]);

    #region Validation Tests
    #endregion Validation Tests

    #region Core Logic & Anchor Time

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Anchor_Time_Should_Be_Inherited_And_Jump_To_Next_Day()
    {
        // Arrange: Request at 10:30 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 10:30 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
        result.Description.ShouldContain("10:30");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_ExecutionDateTimeLocal_Should_Be_Completely_Ignored()
    {
        // Arrange: 8 AM request, 11 PM configured (but should be ignored)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero).AddHours(15), // 11 PM, but should be ignored
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 8 AM
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
    }

    #endregion Core Logic & Anchor Time

    #region Day Skipping (RecursEvery)

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Pattern_Should_Skip_Days_According_To_Recursion_Value()
    {
        // Arrange: Day 01 + every 3 days = Day 04
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 3,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(4);
        result.NextExecutionTime.Value.Hour.ShouldBe(12);
    }

    #endregion Day Skipping (RecursEvery)

    #region FirstDayOfWeek Invariance

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_FirstDayOfWeek_Configuration_Should_Not_Affect_Daily_Calculations_NO_DETERMINISTA()
    {
        // Arrange: The daily strategy should not change if the week starts on Monday or Sunday
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var resMonday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Monday });
        var resSunday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Sunday });

        // Assert
        resMonday.NextExecutionTime.ShouldBe(resSunday.NextExecutionTime);
    }

    #endregion FirstDayOfWeek Invariance

    #region Calendar Edge Cases

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Month_And_Year_Transitions_Should_Work_Seamlessly()
    {
        // Arrange: 31 Dec -> 1 Jan
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Leap_Year_February_29_Should_Be_Handled()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(2);
        result.NextExecutionTime.Value.Day.ShouldBe(29);
    }

    #endregion Calendar Edge Cases

    #region Localization & Description (Using Theories)

    [Theory]
    [InlineData(1, "Occurs every day")]
    [InlineData(3, "Occurs every 3 days")]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Description_Prefix_Should_Reflect_Recursion_Interval(int every, string expectedPrefix)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = every,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);
        
        // Assert
        result.Description.ShouldContain(expectedPrefix);
    }

    #endregion Localization & Description (Using Theories)

    #region Limits & Safety

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Future_StartDateLimit_Should_Be_Respected_With_Anchor_Time()
    {
        // Arrange: Request on day 01 at 10 AM. Limit on day 10.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero).AddDays(9),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_EndDate_Reached_Should_Stop_Motor_And_Return_Failure()
    {
        // Arrange: Today is day 01. Every 10 days (next is day 11). Limit is day 05.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 10,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero).AddDays(4),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };
        
        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
    }

    #endregion Limits & Safety

}