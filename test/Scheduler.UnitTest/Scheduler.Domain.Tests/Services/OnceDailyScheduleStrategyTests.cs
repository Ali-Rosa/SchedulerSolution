using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_OnceDailyScheduleStrategyTests
{
    private readonly SchedulerService _service;
    public CalculateNextExecution_OnceDailyScheduleStrategyTests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void CalculateOnce_WithDateTimeNull_ReturnsSuccess()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder
            .OnceDaily()
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
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(-1);
        var startDate = execution.AddDays(-20);

        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_ExecutionDateTimeLocal(execution)
            .With_Limits_StartDateLocal(startDate)
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
    public void CalculateOnce_WithDatetimeBeforeStartdateDate_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(1);
        var startDate = execution.AddDays(10);

        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_ExecutionDateTimeLocal(execution)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(1);
        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_ExecutionDateTimeLocal(execution)
            .With_Limits_StartDateLocal(execution.AddDays(-10))
            .With_Limits_EndDateLocal(execution.AddDays(-1))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(1);
        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_ExecutionDateTimeLocal(execution)
            .With_Limits_StartDateLocal(execution.AddDays(-10))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(1);
        var config = ScheduleConfigurationBuilder
            .OnceDaily()
            .With_ExecutionDateTimeLocal(execution)
            .With_Limits_StartDateLocal(execution.AddDays(-10))
            .With_Limits_EndDateLocal(execution.AddDays(20))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
            .OnceDaily()
            .With_ExecutionDateTimeLocal(executionDto)
            .With_Limits_StartDateLocal(executionDto.AddDays(-1))
            .With_TimeZoneId(timeZoneId)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(executionDto, result.NextExecutionTime);
    }

}