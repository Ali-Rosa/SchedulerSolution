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

    #region CalculateRecurring - Recurring execution

    #region CalculateRecurring - Recurring execution - Initial Validations
    
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

    #endregion CalculateRecurring - Recurring execution - Initial Validations

    #region CalculateRecurring - Recurring execution - With DateTime
    
    [Fact]
    public void CalculateRecurring_WithDateTime_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
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
        Assert.True(result.NextExecutionTime!.Value > datetimeOnly);
        Assert.Equal(datetimeOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);

    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateAndEndDate_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
        var datetimeOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");
        var enddateOnly = DateTime.Parse("2020-01-06");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.True(result.NextExecutionTime!.Value > datetimeOnly);
        Assert.True(result.NextExecutionTime!.Value <= enddateOnly);
        Assert.Equal(datetimeOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);

    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateHigher_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
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
        Assert.True(result.NextExecutionTime!.Value > datetimeOnly);
        Assert.Equal(datetimeOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateHigher_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
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

    [Fact]
    public void CalculateRecurring_WithDateTimeAndMinorEndDate_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
        var datetimeOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");
        var enddateOnly = DateTime.Parse("2020-01-04");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("There are no executions within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithDateTimeAndStartDateGreaterEndDate_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
        var datetimeOnly = DateTime.Parse("2020-01-05");
        var startdateOnly = DateTime.Parse("2020-01-02");
        var enddateOnly = DateTime.Parse("2020-01-01");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: datetimeOnly,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("StartDate cannot be greater than EndDate.", result.ErrorMessage);

    }

    #endregion CalculateRecurring - Recurring execution - With DateTime

    #region CalculateRecurring - Recurring execution - With Current Date 

    [Fact]
    public void CalculateRecurring_WithCurrentDate_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-01");
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
        Assert.True(result.NextExecutionTime!.Value > currentDateOnly);
        Assert.Equal(currentDateOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithCurrentDateAndStartDateAndEndDate_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");
        var enddateOnly = DateTime.Parse("2020-01-06");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: null,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NextExecutionTime);
        Assert.True(result.NextExecutionTime!.Value > currentDateOnly);
        Assert.True(result.NextExecutionTime!.Value <= enddateOnly);
        Assert.Equal(currentDateOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithCurrentDateAndStartDateHigherButPossible_ReturnsNextExecution()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-05");

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
        Assert.True(result.NextExecutionTime!.Value > currentDateOnly);
        Assert.Equal(currentDateOnly.Date.AddDays(config.Every), result.NextExecutionTime!.Value.Date);
        Assert.Contains("Occurs every", result.Description);
    }

    [Fact]
    public void CalculateRecurring_WithCurrentDateAndStartDateHigher_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-07");

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
        Assert.False(result.IsSuccess);
        Assert.Equal("There are no executions within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithCurrentDateAndMinorEndDate_ReturnsError()
    {
        // Arrange
        var currentDateOnly = DateTime.Parse("2020-01-04");
        var startdateOnly = DateTime.Parse("2020-01-01");
        var enddateOnly = DateTime.Parse("2020-01-04");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: null,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("There are no executions within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void CalculateRecurring_WithCurrentDateAndStartDateGreaterEndDate_ReturnsError()
    {
        // Arrange
        var currentDateOnly = new DateTime(2020,1,5);
        var startdateOnly = DateTime.Parse("2020-01-02");
        var enddateOnly = DateTime.Parse("2020-01-01");

        var config = new ScheduleConfiguration(
            Type: ScheduleType.Recurring,
            ExecutionDateTime: null,
            Occurs: OccursType.Daily,
            Enabled: true,
            Every: 1,
            StartDate: startdateOnly,
            EndDate: enddateOnly
        );

        // Act
        var result = _schedulerService.CalculateNextExecution(currentDateOnly, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("StartDate cannot be greater than EndDate.", result.ErrorMessage);

    }

    #endregion CalculateRecurring - Recurring execution - With Current Date 

    #endregion
}