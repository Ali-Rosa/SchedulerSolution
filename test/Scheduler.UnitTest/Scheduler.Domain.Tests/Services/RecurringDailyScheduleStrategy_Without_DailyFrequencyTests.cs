using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringDaily_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;
    public CalculateNextExecution_RecurringDaily_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Should_ReturnError_When_Culture_Is_Invalid()
    {
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("invalid-culture").Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.False(result.IsSuccess);
        Assert.Contains("not supported by the system", result.ErrorMessage);
    }

    [Fact]
    public void Should_ReturnError_When_RecursEvery_Is_Zero_Or_Less()
    {
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_RecursEvery(0).Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.False(result.IsSuccess);
        Assert.Equal("The Every value must be greater than 0.", result.ErrorMessage);
    }

    [Fact]
    public void Should_Respect_Future_StartDateLimit_And_Use_Midnight()
    {
        var currentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);
        var startLimit = currentDate.AddDays(4); // Day 10
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .With_ExecutionDateTimeLocal(currentDate.AddHours(10)) // Ignored
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.True(result.IsSuccess);
        // Expected result: Day 10 at 00:00 (Midnight)
        var expectedDate = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expectedDate, result.NextExecutionTime);
    }

    [Fact]
    public void Should_Ignore_ExecutionDateTimeLocal_And_Jump_To_Next_Midnight()
    {
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero); // 10:00 AM
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_RecursEvery(1)
            .With_ExecutionDateTimeLocal(currentDate.AddHours(4)) // 02:00 PM Ignored
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // As today at 00:00 has already passed, it should be tomorrow at 00:00
        var expected = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, result.NextExecutionTime);
    }
}