using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringDailyScheduleStrategyTests
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_RecurringDailyScheduleStrategyTests()
    {
        _service = SchedulerServiceFactory.CreateDefault();
    }

    [Fact]
    public void CalculateRecurringDaily_WithEveryLessThanOrEqualToZero_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithEvery(0)
            .WithIntraDayEvery(
                unit: IntraDayFrequencyUnit.Hours,
                every: 2,
                start: new TimeOnly(4, 0),
                end: new TimeOnly(8, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Contains("The Every value must be greater than 0.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDailyFrecuencyIsNull_ReturnsError()
    {
        // Arrange
        var execution = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var currentDate = execution.AddDays(-2);
        var startDate = execution.AddDays(-12);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("Daily Frecuency configuration is required for Daily occurs type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDailyFrecuencyWithOcursEveryInHours_ReturnsSuccess()
    {
        // Arrange
        var execution = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var currentDate = execution.AddDays(-2);
        var startDate = execution.AddDays(-12);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithIntraDayEvery(
                unit: IntraDayFrequencyUnit.Hours,
                every: 2,
                start: new TimeOnly(4, 0),
                end: new TimeOnly(8, 0))
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("Occurs every day. Every 2 hours at 04:00. Starting on 02/05/2026", result.Description);
        Assert.Equal("", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDailyFrecuencyWithOcursEveryInMinutes_ReturnsSuccess()
    {
        // Arrange
        var execution = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var currentDate = execution.AddDays(-2);
        var startDate = execution.AddDays(-12);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithEvery(2)
            .WithIntraDayEvery(
                unit: IntraDayFrequencyUnit.Minutes,
                every: 2,
                start: new TimeOnly(0, 10),
                end: new TimeOnly(0, 10))
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("Occurs every day. Every 2 hours at 04:00. Starting on 02/05/2026", result.Description);
        Assert.Equal("", result.ErrorMessage);
    }


    [Fact]
    public void CalculateRecurringDaily_WithDatetimeBeforeCurrentDate_ReturnsError()
    {
        // Arrange
        var execution = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var currentDate = execution.AddDays(1);
        var startDate = execution.AddDays(-10);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("DateTime cannot be less than CurrentDate", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDatetimeBeforeStartdateDate_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);
        var startDate = DateTimeOffset.UtcNow.AddDays(10);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(startDate.AddDays(1), result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_CurrentDateBeforeStartdateDate_ReturnsSuccess()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(10);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(startDate.AddDays(1), result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDatetimeAfterEndDate_ReturnsError()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-1))
            .WithEndDate(execution.AddDays(-1))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("The execution date is outside the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_CurrentDateAfterEndDate_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithStartDate(DateTimeOffset.UtcNow.AddDays(-1))
            .WithEndDate(DateTimeOffset.UtcNow)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("No valid daily execution found within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurringDaily_WithDateTimeNull_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(currentDate.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithValidFutureDateTime_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-10))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(execution.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithValidFutureCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow.AddDays(1);
        var startDate = DateTimeOffset.UtcNow.AddDays(-10);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(currentDate.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-10))
            .WithEndDate(execution.AddDays(20))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(execution.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithValidDateInRangeWithNoStartDate_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .WithExecution(execution)
            .WithEndDate(execution.AddDays(20))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(execution.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurringDaily_WithOnlyCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(currentDate.AddDays(1), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);

    }

}

