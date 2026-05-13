using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;
using Xunit.Sdk;

namespace Scheduler.Domain.Tests.Validators;

public class ScheduleConfigurationValidatorTests
{
    private readonly SchedulerService _service;

    public ScheduleConfigurationValidatorTests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Negative_RecursEvery_Should_Be_Rejected()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_RecursEvery(-1)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The Every value cannot be negative.");
        result.NextExecutionTime.ShouldBeNull();
    }

    [Fact]
    public void Undefined_ScheduleType_Should_Return_Definition_Error()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Invalid_ScheduleType()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Not defined schedule type.");
    }

    [Fact]
    public void Disabled_Configuration_Should_Return_Disabled_Message()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .Disabled()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The schedule is disabled.");
    }

    [Fact]
    public void Undefined_OccursType_Should_Return_Definition_Error()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Invalid_OccursType()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Not defined occurs type.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Fail_When_Locale_Is_Null_Or_Empty(string? invalidLocale)
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringDaily().Build() with { Locale = invalidLocale };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The Locale is required.");
    }

    #region Validations

    [Fact]
    public void Recurring_Schedule_With_Zero_RecursEvery_Should_Be_Rejected()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(0)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The Every value must be greater than 0.");
    }

    [Fact]
    public void Weekly_Recurring_Without_DaysOfWeek_Should_Be_Rejected()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .Build(); // No days specified

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Weekly configuration is required for Weekly recurring schedules.");
    }

    [Fact]
    public void Weekly_Recurring_With_Days_Should_Succeed_Validation()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_Locale("en-US")
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Wednesday)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Once_Schedule_ExecutionDateTime_Before_StartLimit_Should_Be_Rejected()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The execution date is outside the allowed range.");
    }

    [Fact]
    public void Once_Schedule_ExecutionDateTime_After_EndLimit_Should_Be_Rejected()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var endLimit = new DateTimeOffset(2026, 5, 15, 23, 59, 59, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The execution date is outside the allowed range.");
    }

    [Fact]
    public void Once_Schedule_ExecutionDateTime_Within_Limits_Should_Succeed()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        var endLimit = new DateTimeOffset(2026, 5, 20, 23, 59, 59, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Undefined_IntervalUnit_In_DailyFrequency_Should_Be_Rejected()
    {
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_DailyFrequency_OccursEvery((TimeIntervalUnit)999, 2, new TimeOnly(8, 0), new TimeOnly(18, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Not defined interval unit for daily frequency.");
        result.NextExecutionTime.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Zero_Or_Negative_FrequencyInterval_Should_Be_Rejected(int invalidInterval)
    {
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, invalidInterval, new TimeOnly(8, 0), new TimeOnly(18, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The frequency interval must be greater than 0.");
        result.NextExecutionTime.ShouldBeNull();
    }

    #endregion Validations

}