using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;
    public Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Validation Tests

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Invalid_Culture_Should_Return_Support_Error()
    {
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("invalid-culture").Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported by the system");
    }

    [Theory]
    [InlineData(0, "The Every value must be greater than 0.")]     // Arrested by the Strategy
    [InlineData(-1, "The Every value cannot be negative.")]        // Stopped by the Validator
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_RecursEvery_Zero_Or_Less_Should_Be_Rejected(int invalidValue, string expectedError)
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(invalidValue)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    #endregion

    #region Core Logic & Anchor Time

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Anchor_Time_Should_Be_Inherited_And_Jump_To_Next_Day()
    {
        // Arrange: Request at 10:30 AM
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_ExecutionDateTimeLocal(currentDate.AddHours(15))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Tomorrow at 8 AM
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
    }

    #endregion

    #region Day Skipping (RecursEvery)

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Pattern_Should_Skip_Days_According_To_Recursion_Value()
    {
        // Arrange: Day 01 + every 3 days = Day 04
        var currentDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(4);
        result.NextExecutionTime.Value.Hour.ShouldBe(12);
    }

    #endregion

    #region FirstDayOfWeek Invariance

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_FirstDayOfWeek_Configuration_Should_Not_Affect_Daily_Calculations()
    {
        // The daily strategy should not change if the week starts on Monday or Sunday
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var builder = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").With_RecursEvery(1);

        var resMonday = _service.CalculateNextExecution(currentDate, builder.With_FirstDayOfWeek(DayOfWeek.Monday).Build());
        var resSunday = _service.CalculateNextExecution(currentDate, builder.With_FirstDayOfWeek(DayOfWeek.Sunday).Build());

        resMonday.NextExecutionTime.ShouldBe(resSunday.NextExecutionTime);
    }

    #endregion

    #region Calendar Edge Cases

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Month_And_Year_Transitions_Should_Work_Seamlessly()
    {
        // 31 Dec -> 1 Jan
        var currentDate = new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").With_RecursEvery(1).Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Leap_Year_February_29_Should_Be_Handled()
    {
        var currentDate = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").With_RecursEvery(1).Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(2);
        result.NextExecutionTime.Value.Day.ShouldBe(29);
    }

    #endregion

    #region Localization & Description (Using Theories)

    [Theory]
    [InlineData(1, "Occurs every day")]
    [InlineData(3, "Occurs every 3 days")]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Description_Prefix_Should_Reflect_Recursion_Interval(int every, string expectedPrefix)
    {
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(every)
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.Description.ShouldContain(expectedPrefix);
    }

    #endregion

    #region Limits & Safety

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Future_StartDateLimit_Should_Be_Respected_With_Anchor_Time()
    {
        // Request on day 01 at 10 AM. Limit on day 10.
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var startLimit = currentDate.AddDays(9); // Day 10

        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_EndDate_Reached_Should_Stop_Motor_And_Return_Failure()
    {
        // Today is day 01. Every 10 days (next is day 11). Limit is day 05.
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var endLimit = currentDate.AddDays(4);

        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(10)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions found");
    }

    #endregion

}