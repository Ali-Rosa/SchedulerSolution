using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringDaily_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service;
    public CalculateNextExecution_RecurringDaily_With_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Should_Jump_To_Next_Day_When_Pattern_Matches_But_Hours_Passed()
    {
        // // Today, the 6th, at 10 PM. Pattern every 3 days (6, 9...). Hours 4-8 AM.
        var currentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(3)
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Result: Day 09 at 04:00 AM
        Assert.Equal(9, result.NextExecutionTime!.Value.Day);
        Assert.Equal(4, result.NextExecutionTime.Value.Hour);
    }

    [Fact]
    public void Mode_OccursOnce_Should_Take_Precedence_Over_OccursEvery()
    {
        // Arrange: We created a "dirty" object (both enabled) just for this test
        var frequencyAmbigua = new ScheduleDailyFrecuency(
            OccursOnceEnable: true,
            OnceTime: new TimeOnly(15, 0), // 3 PM
            OccursEveryEnable: true,       // Also enabled (User error)
            IntervalUnit: TimeIntervalUnit.Hours,
            FrequencyInterval: 1,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(10, 0)
        );

        var currentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(1)
            .With_DailyFrecuency(frequencyAmbigua) // We inject the object directly
            .With_Locale("en-US")
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: The 'Once' mode should take precedence (15:00)
        Assert.Equal(15, result.NextExecutionTime!.Value.Hour);
    }

    [Fact]
    public void Mode_OccursEvery_Should_Execute_Correct_Intervals()
    {
        var currentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(1)
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // The first execution of day 06 at 04:00 AM
        Assert.Equal(4, result.NextExecutionTime!.Value.Hour);
    }

    [Fact]
    public void Should_Return_Today_If_One_Time_Is_Still_Future()
    {
        // Today, the 6th, at 10:00 AM. Fixed time: 03:00 PM (15:00).
        var currentDate = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(1)
            .With_DailyFrecuency_OccursOnce(new TimeOnly(15, 0))
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.Equal(6, result.NextExecutionTime!.Value.Day);
        Assert.Equal(15, result.NextExecutionTime.Value.Hour);
    }
}