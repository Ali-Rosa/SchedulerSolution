//using Scheduler.Domain.Models;
//using Scheduler.Domain.Services;
//using Scheduler.Domain.Strategies;
//using Shouldly;

//namespace Scheduler.Domain.Tests;

//public class Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Tests
//{
//    private readonly SchedulerService _service = new([new RecurringDailySchedulerStrategy()]);

//    #region Validation Tests

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Invalid_Culture_Should_Return_Support_Error()
//    {
//        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "invalid-culture"
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.IsSuccess.ShouldBeFalse();
//        result.ErrorMessage.ShouldContain("not supported by the system");
//    }

//    [Theory]
//    [InlineData(0, "The Every value must be greater than 0.")]     // Arrested by the Strategy
//    [InlineData(-1, "The Every value must be greater than 0.")]        // Stopped by the Validator
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_RecursEvery_Zero_Or_Less_Should_Be_Rejected(int invalidValue, string expectedError)
//    {
//        // Arrange
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = invalidValue,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        // Act
//        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

//        // Assert
//        result.IsSuccess.ShouldBeFalse();
//        result.ErrorMessage.ShouldBe(expectedError);
//    }

//    #endregion Validation Tests

//    #region Core Logic & Anchor Time

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Anchor_Time_Should_Be_Inherited_And_Jump_To_Next_Day()
//    {
//        // Arrange: Request at 10:30 AM
//        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            ExecutionDateTimeLocal = currentDate,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert: Tomorrow at 10:30 AM
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(2);
//        result.NextExecutionTime.Value.Hour.ShouldBe(10);
//        result.NextExecutionTime.Value.Minute.ShouldBe(30);
//        result.Description.ShouldContain("10:30");
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_ExecutionDateTimeLocal_Should_Be_Completely_Ignored()
//    {
//        // Arrange: 8 AM request, 11 PM configured (but should be ignored)
//        var currentDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            ExecutionDateTimeLocal = currentDate.AddHours(15), // 11 PM, but should be ignored
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert: Tomorrow at 8 AM
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(2);
//        result.NextExecutionTime.Value.Hour.ShouldBe(8);
//    }

//    #endregion Core Logic & Anchor Time

//    #region Day Skipping (RecursEvery)

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Pattern_Should_Skip_Days_According_To_Recursion_Value()
//    {
//        // Arrange: Day 01 + every 3 days = Day 04
//        var currentDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 3,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(4);
//        result.NextExecutionTime.Value.Hour.ShouldBe(12);
//    }

//    #endregion Day Skipping (RecursEvery)

//    #region FirstDayOfWeek Invariance

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_FirstDayOfWeek_Configuration_Should_Not_Affect_Daily_Calculations_NO_DETERMINISTA()
//    {
//        // The daily strategy should not change if the week starts on Monday or Sunday
//        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var resMonday = _service.CalculateNextExecution(currentDate, config with { FirstDayOfWeek = DayOfWeek.Monday });
//        var resSunday = _service.CalculateNextExecution(currentDate, config with { FirstDayOfWeek = DayOfWeek.Sunday });

//        resMonday.NextExecutionTime.ShouldBe(resSunday.NextExecutionTime);
//    }

//    #endregion FirstDayOfWeek Invariance

//    #region Calendar Edge Cases

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Month_And_Year_Transitions_Should_Work_Seamlessly()
//    {
//        // 31 Dec -> 1 Jan
//        var currentDate = new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero);
        
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Year.ShouldBe(2026);
//        result.NextExecutionTime.Value.Month.ShouldBe(1);
//        result.NextExecutionTime.Value.Day.ShouldBe(1);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Leap_Year_February_29_Should_Be_Handled()
//    {
//        var currentDate = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Month.ShouldBe(2);
//        result.NextExecutionTime.Value.Day.ShouldBe(29);
//    }

//    #endregion Calendar Edge Cases

//    #region Localization & Description (Using Theories)

//    [Theory]
//    [InlineData(1, "Occurs every day")]
//    [InlineData(3, "Occurs every 3 days")]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Description_Prefix_Should_Reflect_Recursion_Interval(int every, string expectedPrefix)
//    {
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = every,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

//        result.Description.ShouldContain(expectedPrefix);
//    }

//    #endregion Localization & Description (Using Theories)

//    #region Limits & Safety

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Future_StartDateLimit_Should_Be_Respected_With_Anchor_Time()
//    {
//        // Request on day 01 at 10 AM. Limit on day 10.
//        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
//        var startLimit = currentDate.AddDays(9); // Day 10

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 1,
//            LimitsStartDateLocal = startLimit,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(10);
//        result.NextExecutionTime.Value.Hour.ShouldBe(10);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_EndDate_Reached_Should_Stop_Motor_And_Return_Failure()
//    {
//        // Today is day 01. Every 10 days (next is day 11). Limit is day 05.
//        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
//        var endLimit = currentDate.AddDays(4);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Daily,
//            RecursEvery = 10,
//            LimitsEndDateLocal = endLimit,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US"
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.IsSuccess.ShouldBeFalse();
//        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
//    }

//    #endregion Limits & Safety

//}