using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();
    
    #region Validation Tests

    [Fact]
    public void Invalid_Culture_Should_Return_Support_Error()
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("invalid-culture")
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported by the system");
    }

    [Theory]
    [InlineData(0, "The Every value must be greater than 0.")]
    [InlineData(-1, "The Every value cannot be negative.")]
    public void Weekly_RecursEvery_Validation_Should_Be_Strict(int invalidValue, string expectedError)
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(invalidValue)
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Fact]
    public void Missing_Weekly_Configuration_Should_Return_Error()
    {
        // The builder without .With_WeeklyDays() leaves the object null
        var config = ScheduleConfigurationBuilder.RecurringWeekly().With_Locale("en-US").Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration");
    }

    [Fact]
    public void Empty_Weekly_Days_List_Should_Return_Error()
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_WeeklyDays() // Empty list
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Weekly configuration and at least one day are required.");
    }

    #endregion Validation Tests

    #region Core Logic & Anchor Time

    [Fact]
    public void Execution_Should_Follow_Anchor_Time_And_Ignore_ExecutionDateTimeLocal()
    {
        // Arrange: Monday 04 at 10:00 AM. 
        var currentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
        var executionTime = currentDate.AddHours(5); // 03:00 PM (Should be ignored)

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_ExecutionDateTimeLocal(executionTime)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Next Monday (11) at 10:00 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(11);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.Description.ShouldContain("10:00");
    }

    [Fact]
    public void Should_Find_Next_Day_In_Same_Week_Maintaining_Anchor_Time()
    {
        // Today Tuesday 05 at 10:00 AM. Days: Friday.
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Friday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(8);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    #endregion Core Logic & Anchor Time

    #region Week Skipping (RecursEvery)

    [Fact]
    public void Pattern_Should_Skip_Weeks_According_To_Recursion_Value()
    {
        // Monday 04 at 00:01. Every 2 weeks, Monday.
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Monday 04 (1 min passed) -> Monday 11 (Week 1, Skip) -> Monday 18 (Week 2)
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(18);
    }

    [Fact]
    public void Should_Find_All_Selected_Days_Only_In_The_Correct_Week_Iteration()
    {
        // Every 3 weeks: Monday, Wednesday, Friday. Start Monday 04.
        var startDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

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
    public void FirstDayOfWeek_Should_Determine_Correct_Week_Group()
    {
        // Start Thursday 07. Evaluate Monday 11. Every 2 weeks.
        var startLimit = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);

        var builder = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_Limits_StartDateLocal(startLimit);

        // Case A: Start Monday -> Monday 11 is Week 1 (Skip) -> Monday 18 (Week 2)
        var resA = _service.CalculateNextExecution(startLimit, builder.With_FirstDayOfWeek(DayOfWeek.Monday).Build());
        resA.NextExecutionTime.ShouldNotBeNull();
        resA.NextExecutionTime.Value.Day.ShouldBe(18);

        // Case B: Start Thursday -> Monday 11 is Week 0 (Hit)
        var resB = _service.CalculateNextExecution(startLimit, builder.With_FirstDayOfWeek(DayOfWeek.Thursday).Build());
        resB.NextExecutionTime.ShouldNotBeNull();
        resB.NextExecutionTime.Value.Day.ShouldBe(11);
    }

    #endregion FirstDayOfWeek Impact

    #region Calendar Edge Cases

    [Fact]
    public void Should_Handle_Year_Transition_Correctly()
    {
        var startDate = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = _service.CalculateNextExecution(startDate.AddMinutes(1), config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Day.ShouldBe(5);
    }

    [Fact]
    public void Should_Handle_Leap_Year_February_Correctly()
    {
        var startDate = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero); // Jueves
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Thursday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = _service.CalculateNextExecution(startDate.AddMinutes(1), config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(3);
        result.NextExecutionTime.Value.Day.ShouldBe(7);
    }

    #endregion Calendar Edge Cases

    #region Localization & Description Tests

    [Fact]
    public void Description_Should_Use_Cultural_Format()
    {
        var currentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("ru-RU")
            .With_WeeklyDays(DayOfWeek.Friday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("2026");
    }

    #endregion Localization & Description Tests

    #region Limits & Safety

    [Fact]
    public void Motor_Should_Stop_At_EndDate_Limit()
    {
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Friday) // Día 08
            .With_Limits_EndDateLocal(currentDate.AddDays(3)) // Día 07
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions found");
    }

    [Fact]
    public void Should_Fail_When_Pattern_Is_Beyond_Safety_Cap()
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_RecursEvery(300) // 300 weeks > 5 years (My limit is 1826 days hardcoded)
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_Null_Weekly_Days_In_Object()
    {
        var frequencyNula = new SchedulerWeekly(DaysOfWeek: null!);
        var config = ScheduleConfigurationBuilder.RecurringWeekly().With_Locale("en-US").Build();
        var configConError = config with { Weekly = frequencyNula };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configConError);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration");
    }

    #endregion Limits & Safety

}