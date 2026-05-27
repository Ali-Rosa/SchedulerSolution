using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_OnceDaily_Tests
{
    private readonly SchedulerService _service = new([new OnceDailySchedulerStrategy()]);

    #region Basic & Default Scenarios

    [Fact]
    public void CalculateNextExecution_OnceDaily_WithExecutionDateTimeLocal_Should_Be_Used_As_NextExecution()
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
        //result.NextExecutionTime.Value.Date.ShouldBe(dateTime.Date);
        //result.NextExecutionTime.Value.Hour.ShouldBe(dateTime.Hour);
        //result.NextExecutionTime.Value.Minute.ShouldBe(dateTime.Minute);
    }

    [Fact]
    public void Calculate_NextExecution_Once_Daily_Without_ExecutionDateTimeLocal_Should_Use_CurrentDate()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);

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
    public void CalculateNextExecution_OnceDaily_WithCurrentDateGreaterThanExecutionDateTimeLocal_ShouldBeReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.Zero),
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


    #endregion Basic & Default Scenarios

    #region Execution Limits

    [Fact]
    public void CalculateNextExecution_OnceDaily_WithExecutionDateTimeLocalLessThanLimitsStartDate_ShouldBeReturnsError()
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
    public void CalculateNextExecution_OnceDaily_WithExecutionDateTimeLocalGreaterThanLimitsEndDate_ShouldBeReturnsError()
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
    public void CalculateNextExecution_OnceDaily_LimitsStartDateEqualToLimitsEndDate_ShouldPassValidation()
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
    public void Calculate_NextExecution_Once_Daily_Past_Execution_Dates_Should_Return_Specific_Error(int hoursBack, string expectedError)
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
    public void Calculate_NextExecution_Once_Daily_Dates_Outside_Start_Or_End_Limits_Should_Fail(int daysToExecution, int daysToLimit)
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

    #endregion Execution Limits

    #region Date/Time Range Limits

    [Fact]
    public void Calculate_NextExecution_Once_Daily_ExecutionDateTimeLocal_Before_LimitsStartDateLocal_Should_Fail()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero); // Before execution
        var startLimit = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.Zero),
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
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
    public void Calculate_NextExecution_Once_Daily_ExecutionDateTimeLocal_Within_Valid_Range_Should_Succeed()
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
    public void Calculate_NextExecution_Once_Daily_Boundary_Execution_At_StartDate_Should_Succeed()
    {
        // Arrange: Execution exactly at the start date boundary
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
    public void Calculate_NextExecution_Once_Daily_Boundary_Execution_At_EndDate_Should_Succeed()
    {
        // Arrange: Execution exactly at the end date boundary
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
    public void Calculate_NextExecution_Once_Daily_Response_Should_Correctly_Convert_To_Local_Time()
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

    [Fact]
    public void Calculate_NextExecution_Once_Daily_Description_Should_Include_Starting_Label_When_Limit_Is_Present()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero).AddDays(-1),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);
        
        // Assert
        result.Description.ShouldContain("starting on");
        result.Description.ShouldContain(config.LimitsStartDateLocal.Value.ToString("dd/MM/yyyy"));
    }

    #endregion TimeZone & Localization

    #region Description Formatting

    [Fact]
    public void Calculate_NextExecution_Once_Daily_Description_Should_Include_StartDate_When_LimitsStartDateLocal_Exists()
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
    public void Calculate_NextExecution_Once_Daily_Description_Should_Not_Include_StartDate_When_LimitsStartDateLocal_Is_Null()
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
    public void Calculate_NextExecution_Once_Daily_Description_Should_Respect_TimeZone_Conversion()
    {
        // Arrange: 10:00 UTC in CST (UTC-6 in winter) = 04:00 CST
        var executionDateTime = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var currentDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero); // Before execution

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
    public void Calculate_NextExecution_Once_Daily_RecursEvery_Should_Be_Ignored_For_Once_Schedule()
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
    public void Calculate_NextExecution_Once_Daily_DailyFrequency_Should_Be_Ignored_For_Once_Schedule()
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