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
        var result = _schedulerService.CalculateNextExecution(
            DateTimeOffset.UtcNow,
            null
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        //Assert.Contains("The configuration cannot be null.", result.Description);
        Assert.Equal("The configuration cannot be null.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithDisabledSchedule_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration
            .DefaultOnce()
            with
        { Enabled = false };

        // Act
        var result = _schedulerService.CalculateNextExecution(
            DateTimeOffset.UtcNow,
            config
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("The schedule is disabled.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedScheduleType_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration
            .DefaultOnce()
            with
        { Type = (ScheduleType)1222 };

        // Act
        var result = _schedulerService.CalculateNextExecution(
            DateTimeOffset.UtcNow,
            config
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Not defined schedule type.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithInvalidTimeZone_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration
            .DefaultOnce()
            with
        { TimeZoneId = "Invalid/Zone" };

        // Act
        var result = _schedulerService.CalculateNextExecution(
            DateTimeOffset.UtcNow,
            config
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.StartsWith("Invalid TimeZoneId", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WhenStrategyIsNotRegistered_ReturnsUnsupportedScheduleTypeError()
    {
        // Arrange
        var strategies = new IScheduleStrategy[]
        {
            new OnceScheduleStrategy()
        };

        var schedulerService = new SchedulerService(strategies);

        var config = ScheduleConfiguration
            .DefaultRecurring();

        // Act
        var result = schedulerService.CalculateNextExecution(
            DateTimeOffset.UtcNow,
            config
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("Unsupported schedule type.", result.ErrorMessage);
    }

    #endregion CalculateNextExecution - Initial Validations


    #region CalculateOnce - Single execution

    [Fact]
    public void CalculateOnce_WithDateTimeNull_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce();

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("ExecutionDateTimeUtc is required for a one-time schedule.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateOnce_WithDatetimeBeforeCurrentDate_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeUtc = DateTimeOffset.UtcNow,
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow.AddDays(1), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.NextExecutionTime);
        Assert.Equal("", result.Description);
        Assert.Equal("DateTime greater than CurrentDate", result.ErrorMessage);
    }

    //[Fact]
    //public void CalculateOnce_WithDateBeforeStartDate_ReturnsError()
    //{
    //    // Arrange
    //    var config = ScheduleConfiguration.DefaultOnce() with
    //    {
    //        ExecutionDateTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
    //        StartDateUtc = DateTimeOffset.UtcNow.AddDays(2)
    //    };

    //    // Act
    //    var result = _schedulerService.CalculateNextExecution(DateTimeOffset.UtcNow, config);

    //    // Assert
    //    Assert.False(result.IsSuccess);
    //    Assert.Equal("The execution date is prior to the start date.", result.ErrorMessage);
    //}

    [Fact]
    public void CalculateOnce_WithDatetimeAfterEndDate_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            EndDateUtc = DateTimeOffset.UtcNow
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
            ExecutionDateTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(config.ExecutionDateTimeUtc.Value, result.NextExecutionTime!.Value);
        Assert.Contains("Occurs once", result.Description);
    }

    [Fact]
    public void CalculateOnce_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var config = ScheduleConfiguration.DefaultOnce() with
        {
            ExecutionDateTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            EndDateUtc = DateTimeOffset.UtcNow.AddDays(2)
        };

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(config.ExecutionDateTimeUtc.Value, result.NextExecutionTime!.Value);
    }


    #endregion CalculateOnce - Single execution







}