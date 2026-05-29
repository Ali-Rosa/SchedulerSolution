using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_GeneralValidationsTests
{
    private readonly SchedulerService _service = new([
        new OnceDailySchedulerStrategy(),
        new RecurringDailySchedulerStrategy(),
        new RecurringWeeklySchedulerStrategy(),
        new RecurringMonthlySchedulerStrategy()
    ]);

    #region General validations

    [Fact]
    public void CalculateNextExecution_WhenConfigIsNull_ReturnsError()
    {
        // Act
        var result = _service.CalculateNextExecution(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The configuration cannot be null.");
    }

    [Fact]
    public void CalculateNextExecution_WhenSchedulerIsDisabled_ReturnsError()
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
    public void CalculateNextExecution_WhenSchedulerTypeIsInvalid_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = (SchedulerType)1981,
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
    public void CalculateNextExecution_WhenOccursTypeIsInvalid_ReturnsError()
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

    [Theory]
    [InlineData(0, "The Every value must be greater than 0.")]
    [InlineData(-1, "The Every value must be greater than 0.")]
    public void CalculateNextExecution_WhenRecursEveryIsZeroOrLess_ReturnsError(int invalidValue, string expectedError)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = invalidValue,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Fact]
    public void CalculateNextExecution_WhenLimitsStartDateIsAfterEndDate_ReturnsError()
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CalculateNextExecution_WhenTimeZoneIdIsNullOrEmpty_ReturnsError(string? invalidTimeZoneId)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = invalidTimeZoneId!,
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
    public void CalculateNextExecution_WhenTimeZoneIdIsInvalid_ReturnsError()
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CalculateNextExecution_WhenLocaleIsNullOrEmpty_ReturnsError(string? invalidLocale)
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
            Locale = invalidLocale!,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith("The Locale is required.");
    }

    [Fact]
    public void CalculateNextExecution_WhenLocaleIsNotSupported_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "invalid-culture"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported by the system");
    }

    [Fact]
    public void CalculateNextExecution_WhenFirstDayOfWeekIsInvalid_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            FirstDayOfWeek = (DayOfWeek)1981,
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

    [Fact]
    public void CalculateNextExecution_WhenNoStrategyMatchesConfiguration_ReturnsError()
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

    [Fact]
    public void CalculateNextExecution_WhenWeeklyConfigurationIsMissing_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = null,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Weekly configuration is required for Weekly recurring schedules.");
    }

    [Fact]
    public void CalculateNextExecution_WhenMonthlyConfigurationIsMissing_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = null,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Monthly configuration is required for Monthly recurring schedules.");
    }


    #endregion General validations

}