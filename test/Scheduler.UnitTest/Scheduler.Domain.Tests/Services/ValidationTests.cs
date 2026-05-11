using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_ValidationTests
{
    private readonly SchedulerService _service;
    public CalculateNextExecution_ValidationTests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void CalculateNextExecution_WithNullConfig_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = _service.CalculateNextExecution(currentDate, null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The configuration cannot be null.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WhenStrategyCombinationIsNotRegistered_ReturnsUnsupportedCombinationError()
    {

        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var schedulerService = new SchedulerService(new IScheduleStrategy[]
        {
            new OnceDailyScheduleStrategy()
        });
        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .Build();

        // Act
        var result = schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Unsupported schedule and occurs combination.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithInvalidTimeZone_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_TimeZoneId("Invalid/Zone")
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.StartsWith("Invalid TimeZoneId", result.ErrorMessage);
    }

}