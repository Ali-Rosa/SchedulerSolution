using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Weekly;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Validation Tests

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Invalid_Culture_Should_Return_Support_Error()
    {
        SchedulerConfiguration config = new() 
        { 
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            LimitsStartDateLocal = DateTimeOffset.UtcNow,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "invalid-culture",
        };    

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported by the system");
    }

    [Theory]
    [InlineData(0, "The Every value must be greater than 0.")]
    [InlineData(-1, "The Every value must be greater than 0.")]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IRecursEvery_Validation_Should_Be_Strict(int invalidValue, string expectedError)
    {
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = invalidValue,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IMissing_Weekly_Configuration_Should_Return_Error()
    {
        // The builder without .With_WeeklyDays() leaves the object null
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IEmpty_Weekly_Days_List_Should_Return_Error()
    {
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [] }, // Empty list
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Weekly configuration requires at least one day.");
    }

    #endregion Validation Tests

    #region Core Logic & Anchor Time

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IExecution_Should_Follow_Anchor_Time_And_Ignore_ExecutionDateTimeLocal()
    {
        // Arrange: Monday 04 at 10:00 AM. 
        var currentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
        var executionTime = currentDate.AddHours(5); // 03:00 PM (Should be ignored)

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            ExecutionDateTimeLocal = executionTime, // Should be ignored
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
            
        };

        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Next Monday (11) at 10:00 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(11);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.Description.ShouldContain("10:00");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Find_Next_Day_In_Same_Week_Maintaining_Anchor_Time()
    {
        // Today Tuesday 05 at 10:00 AM. Days: Friday.
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(8);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    #endregion Core Logic & Anchor Time

    #region Week Skipping (RecursEvery)

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IPattern_Should_Skip_Weeks_According_To_Recursion_Value()
    {
        // Monday 04 at 00:01. Every 2 weeks, Monday.
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
             RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(currentDate, config);

        // Monday 04 (1 min passed) -> Monday 11 (Week 1, Skip) -> Monday 18 (Week 2)
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(18);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Find_All_Selected_Days_Only_In_The_Correct_Week_Iteration()
    {
        // Every 3 weeks: Monday, Wednesday, Friday. Start Monday 04.
        var startDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 3,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday] },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // 1. Monday 04 at noon -> Wednesday 06 (Same week 0)
        var res1 = _service.CalculateNextExecution(startDate.AddHours(12), config);
        res1.NextExecutionTime.ShouldNotBeNull();
        res1.NextExecutionTime.Value.Day.ShouldBe(6);

        // 2. Friday 08 at noon -> Skip weeks 1 and 2 -> Monday 25 (Week 3)
        var res2 = _service.CalculateNextExecution(startDate.AddDays(4).AddHours(12), config);
        res2.NextExecutionTime.ShouldNotBeNull();
        res2.NextExecutionTime.Value.Day.ShouldBe(25);
    }

    #endregion Week Skipping (RecursEvery)

    #region FirstDayOfWeek Impact

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IFirstDayOfWeek_Should_Determine_Correct_Week_Group()
    {
        // Start Thursday 07. Evaluate Monday 11. Every 2 weeks.
        var startLimit = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = startLimit,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Case A: Start Monday -> Monday 11 is Week 1 (Skip) -> Monday 18 (Week 2)
        var resA = _service.CalculateNextExecution(startLimit, config with { FirstDayOfWeek = DayOfWeek.Monday });
        resA.NextExecutionTime.ShouldNotBeNull();
        resA.NextExecutionTime.Value.Day.ShouldBe(18);

        // Case B: Start Thursday -> Monday 11 is Week 0 (Hit)
        var resB = _service.CalculateNextExecution(startLimit, config with { FirstDayOfWeek = DayOfWeek.Thursday });
        resB.NextExecutionTime.ShouldNotBeNull();
        resB.NextExecutionTime.Value.Day.ShouldBe(11);
    }

    #endregion FirstDayOfWeek Impact

    #region Calendar Edge Cases

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Handle_Year_Transition_Correctly()
    {
        var startDate = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(startDate.AddMinutes(1), config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Day.ShouldBe(5);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Handle_Leap_Year_February_Correctly()
    {
        var startDate = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero); // Jueves
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Thursday] },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(startDate.AddMinutes(1), config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(3);
        result.NextExecutionTime.Value.Day.ShouldBe(7);
    }

    #endregion Calendar Edge Cases

    #region Localization & Description Tests

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IDescription_Should_Use_Cultural_Format()
    {
        var currentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "ru-RU", // Russian culture
        };

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("2026");
    }

    #endregion Localization & Description Tests

    #region Limits & Safety

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IMotor_Should_Stop_At_EndDate_Limit()
    {
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] }, // Next Friday is 2026-05-08
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Before the next execution
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Reject_Null_Weekly_Days_In_Object()
    {
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = null! },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration");
    }

    #endregion Limits & Safety

}