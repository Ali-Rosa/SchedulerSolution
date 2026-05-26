using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class Calculate_NextExecution_GeneralsValidations_Tests
{
    private readonly SchedulerService _service = new([
        new OnceDailySchedulerStrategy(),
        new RecurringDailySchedulerStrategy(),
        new RecurringWeeklySchedulerStrategy(),
        new RecurringMonthlySchedulerStrategy()
    ]);

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_With_Null_Config_ShouldBeReturnsError()
    {
        // Act
        var result = _service.CalculateNextExecution(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The configuration cannot be null.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithSchedulerConfigurationDisabled_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = false,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("The schedule is disabled.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithUndefinedSchedulerType_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = (SchedulerType)999,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Not defined schedule type.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithUndefinedOccursType_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = (OccursType)999,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Not defined occurs type.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithRecursEveryLessThanOrEqualToZero_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 0,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("The Every value must be greater than 0.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_LimitsStartDateAfterLimitsEndDate_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Within the limits, the start date cannot be later than the end date.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithNullOrEmptyTimeZone_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = null!,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("The TimeZoneId is required.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithNullOrEmptyLocale_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = null!,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("The Locale is required.");
    }

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithUndefinedFirstDayOfWeek_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            FirstDayOfWeek = (DayOfWeek)999,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The provided FirstDayOfWeek is not a valid day of the week.");
    }





    ////////////////////////////////////////////////////////////////////////

    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WithInvalidTimeZone_ShouldBeReturnsError()
    {
        // Arrange

        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = "Invalid/Zone_Name",
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("Invalid TimeZoneId");
    }


    [Fact]
    public void CalculateNextExecution_GeneralsValidations_WhenStrategyCombinationIsNotRegistered_ShouldBeReturnsUnsupportedCombinationError()
    {
        // Arrange

        var schedulerService = new SchedulerService(new ISchedulerStrategy[]
        {
            new RecurringDailySchedulerStrategy()
        });

        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = schedulerService.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Unsupported schedule and occurs combination.");
    }

}