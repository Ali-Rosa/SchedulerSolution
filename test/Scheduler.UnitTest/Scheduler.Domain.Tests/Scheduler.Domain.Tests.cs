using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Tests;

public class SchedulerServiceTests
{
    private readonly SchedulerService _schedulerService;

    public SchedulerServiceTests()
    {
        var strategies = new IScheduleStrategy[]
        {
        new OnceScheduleStrategy(),
        new RecurringScheduleStrategy()
        };

        _schedulerService = new SchedulerService(strategies);
    }

    #region CalculateNextExecution - Initial Validations

    [Fact]
    public void CalculateNextExecution_WithNullConfig_ReturnsError()
    {
        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The configuration cannot be null.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithDisabledSchedule_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration .DefaultOnce() with
        { 
            Enabled = false 
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The schedule is disabled.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedScheduleType_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration .DefaultOnce() with
        { 
            Type = (ScheduleType)1222 
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Not defined schedule type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedOccursType_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration .DefaultOnce() with
        { 
            Occurs = (OccursType)1999 
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Not defined occurs type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WhenStrategyCombinationIsNotRegistered_ReturnsUnsupportedCombinationError()
    {
        // Arrange
        var strategies = new IScheduleStrategy[]
        {
            new OnceScheduleStrategy()
        };

        var schedulerService = new SchedulerService(strategies);

        var config = ScheduleConfiguration.DefaultRecurring();

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
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            Every = -1
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The Every value cannot be negative.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithInvalidTimeZone_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration .DefaultOnce() with
        { 
            TimeZoneId = "Invalid/Zone" 
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.StartsWith("Invalid TimeZoneId", result.ErrorMessage);
    }

    #endregion CalculateNextExecution - Initial Validations


    #region CalculateOnce - Single execution
    
    [Fact]
    public void CalculateOnce_WithDateTimeNull_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce();
        var currentDate = DateTimeOffset.UtcNow;

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

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
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow,
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

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
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

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
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-1),
            EndDateLocal = DateTimeOffset.UtcNow
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

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
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(config.ExecutionDateTimeLocal.Value, result.NextExecutionTime!.Value);
        Assert.Contains("Occurs once", result.Description);
    }

    [Fact]
    public void CalculateOnce_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
            EndDateLocal = DateTimeOffset.UtcNow.AddDays(20)
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(config.ExecutionDateTimeLocal.Value, result.NextExecutionTime!.Value);
    }

    [Fact]
    public void CalculateOnce_WithLocalTimeZone_ReturnsSuccess()
    {
        // Arrange
        var timeZoneId = "Europe/Madrid";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var localExecution = new DateTime(2026, 05, 10, 08, 30, 00, DateTimeKind.Unspecified);
        var executionDto = new DateTimeOffset(localExecution, timeZone.GetUtcOffset(localExecution));

        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeLocal = executionDto,
            StartDateLocal = executionDto.AddDays(-1),
            TimeZoneId = timeZoneId
        };

        var nowUtc = DateTimeOffset.UtcNow;

        // Act
        var result = _schedulerService.CalculateNextExecution(nowUtc, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(executionDto, result.NextExecutionTime);
    }


    #endregion CalculateOnce - Single execution


    #region CalculateRecurring - Recurring execution


    [Fact]
    public void CalculateRecurring_WithEveryLessThanOrEqualToZero_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            Every = 0
        };
        var currentDate = DateTimeOffset.UtcNow;

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

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
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow,
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

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
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(config.StartDateLocal.Value.AddDays(config.Every), result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_CurrentDateBeforeStartdateDate_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(config.StartDateLocal.Value.AddDays(config.Every), result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithDatetimeAfterEndDate_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-1),
            EndDateLocal = DateTimeOffset.UtcNow
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

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
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-1),
            EndDateLocal = DateTimeOffset.UtcNow
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

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
        var config = ScheduleConfiguration.DefaultRecurring();
        var currentDate = DateTimeOffset.UtcNow;

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(currentDate.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithValidFutureDateTime_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(config.ExecutionDateTimeLocal.Value.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithValidFutureCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow.AddDays(1);
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(currentDate.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            StartDateLocal = DateTimeOffset.UtcNow.AddDays(-10),
            EndDateLocal = DateTimeOffset.UtcNow.AddDays(20)
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(config.ExecutionDateTimeLocal.Value.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithValidDateInRangeWithNoStartDate_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultRecurring() with
        {
            ExecutionDateTimeLocal = DateTimeOffset.UtcNow.AddDays(1),
            EndDateLocal = DateTimeOffset.UtcNow.AddDays(20)
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(config.ExecutionDateTimeLocal.Value.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithOnlyCurrentDate_ReturnsSuccess()
    {
        // Arrange
        var currentDate = DateTimeOffset.UtcNow;
        var config = ScheduleConfiguration.DefaultRecurring();
;

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal("", result.ErrorMessage);
        Assert.Equal(currentDate.AddDays(config.Every), result.NextExecutionTime);
        Assert.Contains("Occurs every day. Schedule will be used on ", result.Description);
    }
    #endregion CalculateRecurring - Recurring execution

}