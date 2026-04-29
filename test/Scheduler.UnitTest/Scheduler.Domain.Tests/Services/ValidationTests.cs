using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_ValidationTests
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_ValidationTests()
    {
        _service = SchedulerServiceFactory.CreateDefault();
    }

    [Fact]
    public void CalculateNextExecution_WithNullConfig_ReturnsError()
    {
        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The configuration cannot be null.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithDisabledSchedule_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Once()
            .Disabled()
            .Build();


        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The schedule is disabled.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedScheduleType_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Once()
            .WithInvalidScheduleType()
            .Build();


        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Not defined schedule type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedOccursType_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Once()
            .WithInvalidOccursType()
            .Build();


        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Not defined occurs type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WhenStrategyCombinationIsNotRegistered_ReturnsUnsupportedCombinationError()
    {

        // Arrange
        var schedulerService = new SchedulerService(new IScheduleStrategy[]
        {
            new OnceScheduleStrategy() // Recurring NOT registered
        });

        var config = ScheduleConfigurationBuilder
            .Recurring()
            .Build();

        // Act
        var result = schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Unsupported schedule and occurs combination.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithEveryNegative_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Once()
            .WithEvery(-1)
            .Build();


        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The Every value cannot be negative.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithInvalidTimeZone_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Once()
            .WithTimeZone("Invalid/Zone")
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.StartsWith("Invalid TimeZoneId", result.ErrorMessage);
    }


}