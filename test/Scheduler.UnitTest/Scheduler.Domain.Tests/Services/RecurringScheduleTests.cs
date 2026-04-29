using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers;
using Scheduler.Domain.Tests.TestHelpers.Builders;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringScheduleTests
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_RecurringScheduleTests()
    {
        _service = SchedulerServiceFactory.CreateDefault();
    }

    [Fact]
    public void CalculateRecurring_WithEveryLessThanOrEqualToZero_ReturnsError()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .Recurring()
            .WithEvery(0)
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
    public void CalculateRecurring_WithDatetimeBeforeCurrentDate_ReturnsError()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow;
        var startDate = execution.AddDays(-10);

        var config = ScheduleConfigurationBuilder
            .Recurring()
            .WithExecution(execution)
            .WithStartDate(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("DateTime cannot be less than CurrentDate", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithDatetimeBeforeStartdateDate_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);
        var startDate = DateTimeOffset.UtcNow.AddDays(10);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_CurrentDateBeforeStartdateDate_ReturnsSuccess()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(10);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithDatetimeAfterEndDate_ReturnsError()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_CurrentDateAfterEndDate_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder
            .Recurring()
            .WithStartDate(DateTimeOffset.UtcNow.AddDays(-1))
            .WithEndDate(DateTimeOffset.UtcNow)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("The execution date is outside the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithDateTimeNull_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithValidFutureDateTime_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithValidFutureCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow.AddDays(1);
        var startDate = DateTimeOffset.UtcNow.AddDays(-10);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithValidDateInRangeWithNoStartDate_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Recurring()
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
    public void CalculateRecurring_WithOnlyCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .Recurring()
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

