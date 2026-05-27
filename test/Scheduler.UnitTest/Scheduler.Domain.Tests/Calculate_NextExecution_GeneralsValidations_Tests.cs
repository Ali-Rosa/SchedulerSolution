using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
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

    [Theory]
    [InlineData(0, "The Every value must be greater than 0.")]
    [InlineData(-1, "The Every value must be greater than 0.")]
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_RecursEvery_Zero_Or_Less_Should_Be_Rejected(int invalidValue, string expectedError)
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
    public void Calculate_NextExecution_Recurring_Daily_Without_DailyFrequency_Invalid_Culture_Should_Return_Support_Error()
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


    ///// Validaciones de Integridad para Daily frecuency configuration
    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Description_Should_Include_Detailed_Frequency_InformationLLL()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = -1,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("The frequency interval must be greater than 0.");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Daily_Description_Should_Include_Detailed_Frequency_InformationLLLOO()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = (TimeIntervalUnit)999, // Valor no definido para TimeIntervalUnit
                FrequencyInterval = 1,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Not defined interval unit for daily frequency.");
    }

    //////////////////////////////// Validaciones de Integridad para Weekly configuration
    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IMissing_Weekly_Configuration_Should_Return_Error()
    {
        // The builder without .With_WeeklyDays() leaves the object null
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration is required for Weekly recurring schedules.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new object[] { new DayOfWeek[0] })]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Invalid_Weekly_Days_Should_Return_Error(DayOfWeek[]? daysOfWeek)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = daysOfWeek! },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Weekly configuration requires at least one day.");
    }



}