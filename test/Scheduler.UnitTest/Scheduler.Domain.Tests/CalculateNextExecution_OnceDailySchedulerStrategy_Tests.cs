using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_OnceDailySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new OnceDailySchedulerStrategy()]);

    #region Specific validations OnceDailyStrategy

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsBeforeCurrentDate_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("DateTime cannot be less than CurrentDate.");
    }

    [Fact]
    public void CalculateNextExecution_WhenCandidateIsBeforeStartLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The selected execution date is earlier than the allowed start limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenCandidateIsAfterEndLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The selected execution date is later than the allowed end limit date.");
    }



    #endregion Specific validations OnceDailyStrategy

    #region Basic & Default Scenarios

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsAfterCurrentDate_ReturnsExecutionDateTime()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("15/05/2026");
        result.Description.ShouldContain("14:30");
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsNotProvided_UsesCurrentDate()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.CurrentDate);
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("12/05/2026");
        result.Description.ShouldContain("10:00");
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsCurrentDate_ReturnsExecutionDateTime()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }


    #endregion Basic & Default Scenarios

    #region Execution Limits

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeIsBeforeStartLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The selected execution date is earlier than the allowed start limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeIsAfterEndLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 30, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 30, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The selected execution date is later than the allowed end limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenStartAndEndLimitsAreEqual_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
        result.ErrorMessage.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "DateTime cannot be less than CurrentDate.")] // 1 hour ago
    public void CalculateNextExecution_WhenExecutionDateTimeIsInPast_ReturnsError(int hoursBack, string expectedError)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero).AddHours(-hoursBack),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Theory]
    [InlineData(10, 5)]  // Execution in 10 days, Limit in 5 (Exceeds end)
    [InlineData(2, 5)]   // Execution in 2 days, Start Limit in 5 days (Fails for being before start)
    public void CalculateNextExecution_WhenExecutionDateTimeIsOutsideLimits_ReturnsError(int daysToExecution, int daysToLimit)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero).AddDays(daysToExecution),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Modificación correcta usando la expresión 'with'
        if (daysToExecution < daysToLimit)
            config = config with { LimitsStartDateLocal = config.CurrentDate.AddDays(daysToLimit) };
        else
            config = config with { LimitsEndDateLocal = config.CurrentDate.AddDays(daysToLimit) };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        if (daysToExecution < daysToLimit)
            result.ErrorMessage.ShouldBe("The selected execution date is earlier than the allowed start limit date.");
        else
            result.ErrorMessage.ShouldBe("The selected execution date is later than the allowed end limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateIsNotProvidedAndCurrentDateIsBeforeStartLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The selected execution date is earlier than the allowed start limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateIsNotProvidedAndCurrentDateIsAfterEndLimit_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The selected execution date is later than the allowed end limit date.");
    }

    [Fact]
    public void CalculateNextExecution_WhenOnlyStartLimitExistsAndExecutionIsValid_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }

    [Fact]
    public void CalculateNextExecution_WhenOnlyEndLimitExistsAndExecutionIsValid_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }

    #endregion Execution Limits

    #region Date/Time Range Limits

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeIsWithinLimits_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 14, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 20, 23, 59, 59, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }


    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsStartLimit_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsEndLimit_ReturnsSuccess()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }

    #endregion Date/Time Range Limits

    #region TimeZone & Localization

    [Fact]
    public void CalculateNextExecution_WhenTimeZoneIsApplied_ConvertsToLocalTime()
    {
        // Arrange: 10:00 AM UTC. Madrid in May is UTC+2.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = "Romance Standard Time",
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(12); // 10 + 2
        result.NextExecutionTime.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
    }

    #endregion TimeZone & Localization

    #region Description Formatting

    [Fact]
    public void CalculateNextExecution_WhenStartLimitExists_IncludesStartDateInDescription()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("15/05/2026");
        result.Description.ShouldContain("starting on 10/05/2026");
    }

    [Fact]
    public void CalculateNextExecution_WhenStartLimitDoesNotExist_ExcludesStartDateFromDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotContain("starting on");
    }

    [Fact]
    public void CalculateNextExecution_WhenTimeZoneIsApplied_DescriptionUsesLocalTime()
    {
        // Arrange: 10:00 UTC in CST (UTC-6 in winter) = 04:00 CST
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero), // Before execution
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // The result should be converted to CST time (10:00 UTC - 6 hours = 04:00 CST)
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // The description should show the local time (4:00 AM)
        result.Description.ShouldContain("04:00");
    }

    #endregion Description Formatting

    #region Logical Consistency

    [Fact]
    public void CalculateNextExecution_WhenRecursEveryIsProvided_IgnoresForOnceSchedule()
    {
        // Arrange: RecursEvery value should be irrelevant
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 500,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }

    [Fact]
    public void CalculateNextExecution_WhenDailyFrequencyIsProvided_IgnoresForOnceSchedule()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(18, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Only one execution, not multiple
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTimes.Count().ShouldBe(1);
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }

    #endregion Logical Consistency
}