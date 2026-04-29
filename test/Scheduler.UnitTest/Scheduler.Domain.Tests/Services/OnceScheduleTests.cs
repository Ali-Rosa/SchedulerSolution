using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_OnceScheduleTests
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_OnceScheduleTests()
    {
        _service = SchedulerServiceFactory.CreateDefault();
    }

    [Fact]
    public void CalculateOnce_WithDateTimeNull_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;

        var config = ScheduleConfigurationBuilder
            .Once()
            .Build();


        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(currentDate, result.NextExecutionTime);
        Assert.Contains("Occurs once. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateOnce_WithDatetimeBeforeCurrentDate_ReturnsError()
    {

        // Arrange
        var execution = DateTimeOffset.UtcNow;
        var startDate = execution.AddDays(-10);

        var config = ScheduleConfigurationBuilder
            .Once()
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
    public void CalculateOnce_WithDatetimeBeforeStartdateDate_ReturnsError()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);
        var startDate = execution.AddDays(9);

        var config = ScheduleConfigurationBuilder
            .Once()
            .WithExecution(execution)
            .WithStartDate(startDate)
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
    public void CalculateOnce_WithDatetimeAfterEndDate_ReturnsError()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Once()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-2))
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
    public void CalculateOnce_WithValidFutureDate_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Once()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-10))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(execution, result.NextExecutionTime);
        Assert.Contains("Occurs once", result.Description);
    }

    [Fact]
    public void CalculateOnce_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var execution = DateTimeOffset.UtcNow.AddDays(1);

        var config = ScheduleConfigurationBuilder
            .Once()
            .WithExecution(execution)
            .WithStartDate(execution.AddDays(-10))
            .WithEndDate(execution.AddDays(20))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(execution, result.NextExecutionTime);
    }

    [Fact]
    public void CalculateOnce_WithLocalTimeZone_ReturnsSuccess()
    {
        // Arrange
        var timeZoneId = "Europe/Madrid";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var localExecution = new DateTime(2026, 05, 10, 08, 30, 00, DateTimeKind.Unspecified);
        var executionDto = new DateTimeOffset(localExecution, timeZone.GetUtcOffset(localExecution));

        var config = ScheduleConfigurationBuilder
            .Once()
            .WithExecution(executionDto)
            .WithStartDate(executionDto.AddDays(-1))
            .WithTimeZone(timeZoneId)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(executionDto, result.NextExecutionTime);
    }

}