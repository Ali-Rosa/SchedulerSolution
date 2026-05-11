using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringWeekly_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_RecurringWeekly_With_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Sequence Progression

    [Theory]
    // Stage: Wednesday, January 1, 2020. Days: Mon, Wed, Fri. Hours: 4, 6, 8 AM.
    [InlineData(5, 1, 6)]  // It's 5 AM -> Next is today at 6 AM
    [InlineData(7, 1, 8)]  // It's 7 AM -> Next is today at 8 AM
    [InlineData(22, 3, 4)] // It's 10 PM -> Next is Friday 03 at 4 AM
    public void Weekly_IntraDay_Sequence_Should_Navigate_Correct_Moments(int currentHour, int expectedDay, int expectedHour)
    {
        // Arrange
        var currentDate = new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly("en-US")
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime?.Day.ShouldBe(expectedDay);
        result.NextExecutionTime?.Hour.ShouldBe(expectedHour);
    }

    #endregion Sequence Progression

    #region Complex Week Jumping

    [Fact]
    public void Should_Jump_Multiple_Weeks_Forward_Correctly()
    {
        // Friday, January 3, 2020 at 10 PM. Pattern: Every 2 weeks, Monday/Friday.
        var currentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly("en-US")
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Friday)
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .With_FirstDayOfWeek(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Week 0 ends. Week 1 is skipped. Week 2 starts on Monday 13 at 4 AM.
        result.NextExecutionTime?.Day.ShouldBe(13);
        result.NextExecutionTime?.Hour.ShouldBe(4);
    }

    #endregion Complex Week Jumping

    #region Mode Precedence

    [Fact]
    public void Mode_OccursOnce_Should_Take_Precedence_Over_OccursEvery()
    {
        var frequencyAmbigua = new ScheduleDailyFrecuency(
            OccursOnceEnable: true, OnceTime: new TimeOnly(15, 0),
            OccursEveryEnable: true, IntervalUnit: TimeIntervalUnit.Hours, FrequencyInterval: 2,
            StartTime: new TimeOnly(4, 0), EndTime: new TimeOnly(8, 0)
        );

        var currentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly("en-US")
            .With_WeeklyDays(DayOfWeek.Wednesday)
            .With_DailyFrecuency(frequencyAmbigua)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.NextExecutionTime?.Hour.ShouldBe(15);
        result.NextExecutionTime?.Day.ShouldBe(1);
    }

    #endregion Mode Precedence

    #region Limits & Safety

    [Fact]
    public void Should_Respect_EndDate_Limit()
    {
        var currentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly("en-US")
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_Limits_EndDateLocal(currentDate.AddDays(4))
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions found");
    }

    [Fact]
    public void Should_Handle_Null_DaysOfWeek_Safely()
    {
        var frequencyNula = new ScheduleWeekly(DaysOfWeek: null!);
        var config = ScheduleConfigurationBuilder.RecurringWeekly("en-US").Build();
        var configConError = config with { Weekly = frequencyNula };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configConError);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration");
    }

    #endregion Limits & Safety
}