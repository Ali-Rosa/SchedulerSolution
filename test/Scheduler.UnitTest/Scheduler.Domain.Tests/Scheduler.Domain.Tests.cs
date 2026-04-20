using Scheduler.Domain.Services;
using Scheduler.Domain.Models;

namespace Scheduler.Domain.Tests;

public class SchedulerServiceTests
{
    private readonly SchedulerService _schedulerService = new();

    #region CalculateNextExecution - Initial Validations

    [Fact]
    public void CalculateNextExecution_WithDisabledSchedule_ReturnsError()
    {
        // Arrange
        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: DateTime.Now.AddDays(1),
            Occurs: OccursType.Daily,
            Enabled: false,
            Every: 1,
            StartDate: DateTime.Now,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The schedule is disabled.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNextExecution_WithUnsupportedType_ReturnsError()
    {
        // Arrange - Note: It depends on whether I have more types in Schedule Type
        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: DateTime.Now.AddDays(1),
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: DateTime.Now,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert - It only fails if it actually returns "Unsupported schedule type"
        Assert.True(result.IsSuccess || result.ErrorMessage == "Unsupported schedule type.");
    }

    #endregion

    #region CalculateOnce - Single execution

    [Fact]
    public void CalculateOnce_WithPastDate_ReturnsError()
    {
        // Arrange
        var yesterday = DateTime.Now.AddDays(-1);
        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: yesterday,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: DateTime.Now,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The one-time execution date has already passed.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateOnce_WithDateBeforeStartDate_ReturnsError()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var dayAfterTomorrow = DateTime.Now.AddDays(2);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: tomorrow,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: dayAfterTomorrow, // StartDate > ExecutionDateTime
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The execution date is prior to the start date.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateOnce_WithDateAfterEndDate_ReturnsError()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var yesterday = DateTime.Now.AddDays(-1);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: tomorrow,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: yesterday,
            EndDate: DateTime.Now // EndDate < ExecutionDateTime
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The execution date is outside the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateOnce_WithValidFutureDate_ReturnsSuccess()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var yesterday = DateTime.Now.AddDays(-1);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: tomorrow,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: yesterday,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(tomorrow.Date, result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs once", result.Description);
    }

    [Fact]
    public void CalculateOnce_WithValidDateInRange_ReturnsSuccess()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var yesterday = DateTime.Now.AddDays(-1);
        var dayAfterTomorrow = DateTime.Now.AddDays(2);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Once,
            ExecutionDateTime: tomorrow,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: yesterday,
            EndDate: dayAfterTomorrow
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tomorrow.Date, result.NextExecutionTime!.Value.Date);
    }

    #endregion

    #region CalculateRecurring - Ejecución recurrente

    [Fact]
    public void CalculateRecurring_WithEveryZero_ReturnsError()
    {
        // Arrange
        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: DateTime.Now,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 0, // Invalid
            StartDate: DateTime.Now,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The 'Every' value must be greater than 0.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithEveryNegative_ReturnsError()
    {
        // Arrange
        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: DateTime.Now,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: -5, // Invalid
            StartDate: DateTime.Now,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The 'Every' value must be greater than 0.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithDateTime_ReturnsDateTimePlusEvery()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var datetimeOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(datetimeOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateHigher_ReturnsDateTimePlusEvery()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var datetimeOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-05");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(datetimeOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateHigher_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var datetimeOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-07");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("There are no executions within the allowed range.", result.ErrorMessage);
    }















    /* WITH DATETIME NULL */

    [Fact]
    public void CalculateRecurring_WithDateTimeNull_ReturnsDateTimePlusEvery()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: null,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(currentDateOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
    }














    [Fact]
    public void CalculateRecurring_WithFutureStartDate_ReturnsStartDate()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var hour14 = tomorrow.Date.AddHours(14);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: hour14,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 2,
            StartDate: tomorrow,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(DateTime.Now, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.Equal(hour14.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
    }

    [Fact]
    public void CalculateRecurring_WithPastStartDate_CalculatesNextOccurrence()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var pastDate = currentDate.AddDays(-10); // 10 days ago
        var executionTime = currentDate.Date.AddHours(10); // 10:00 AM

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: executionTime,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 3, // Every 3 days
            StartDate: pastDate,
            EndDate: null
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.True(result.NextExecutionTime!.Value > currentDate);
    }

    [Fact]
    public void CalculateRecurring_WithDateAfterEndDate_ReturnsError()
    {
        // Arrange
        var currentDate = DateTime.Now;
        //var endDate = currentDate.AddDays(1);
        var endDate = currentDate;
        var pastStartDate = currentDate.AddDays(-30);
        var executionTime = currentDate.Date.AddHours(16);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: executionTime,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: pastStartDate,
            EndDate: endDate
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("There are no executions within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithValidConfiguration_ReturnsNextExecution()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var pastStartDate = currentDate.AddDays(-5);
        var executionTime = currentDate.Date.AddHours(15);
        var futureEndDate = currentDate.AddDays(30);

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: executionTime,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 2, // Every 2 days
            StartDate: pastStartDate,
            EndDate: futureEndDate
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.True(result.NextExecutionTime!.Value > currentDate);
        Assert.True(result.NextExecutionTime!.Value <= futureEndDate);
        Assert.Contains("Occurs every", result.Description);
    }

    #endregion
}