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

    [Theory]
    [InlineData("en-US", "The execution date cannot be in the past relative to the current date.")]
    [InlineData("en-GB", "The execution date cannot be in the past relative to the current date.")]
    [InlineData("es-ES", "La fecha de ejecución no puede estar en el pasado con respecto a la fecha actual.")]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsBeforeCurrentDate_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }

    #endregion Specific validations OnceDailyStrategy

    #region Basic & Default Scenarios

    [Theory]
    [InlineData("en-US", "Occurs once", "05-15-2026", "2:30 PM")]
    [InlineData("en-GB", "Occurs once", "15/05/2026", "14:30")]
    [InlineData("es-ES", "Ocurre una vez", "15/05/2026", "14:30")]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsAfterCurrentDate_ReturnsExecutionDateTime(
            string locale,
            string expectedPrefix,
            string expectedDate,
            string expectedTime )
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedDate);
        result.Description.ShouldContain(expectedTime);
    }


    [Theory]
    [InlineData("en-US", "Occurs once", "05-12-2026", "10:00 AM")]
    [InlineData("en-GB", "Occurs once", "12/05/2026", "10:00")]
    [InlineData("es-ES", "Ocurre una vez", "12/05/2026", "10:00")]
    public void CalculateNextExecution_WhenExecutionDateTimeLocalIsNotProvided_UsesCurrentDate(
            string locale,
            string expectedPrefix,
            string expectedDate,
            string expectedTime )
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.CurrentDate);
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedDate);
        result.Description.ShouldContain(expectedTime);
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsCurrentDate_ReturnsExecutionDateTime(string locale)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }


    #endregion Basic & Default Scenarios

    #region Execution Limits

    [Theory]
    [InlineData("en-US", "The selected execution date is earlier than the allowed start limit date.")]
    [InlineData("en-GB", "The selected execution date is earlier than the allowed start limit date.")]
    [InlineData("es-ES", "La fecha de ejecución seleccionada es anterior a la fecha límite de inicio permitida.")]
    public void CalculateNextExecution_WhenExecutionDateTimeIsBeforeStartLimit_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    [InlineData("en-US", "The selected execution date is later than the allowed end limit date.")]
    [InlineData("en-GB", "The selected execution date is later than the allowed end limit date.")]
    [InlineData("es-ES", "La fecha de ejecución seleccionada es posterior a la fecha límite de fin permitida.")]
    public void CalculateNextExecution_WhenExecutionDateTimeIsAfterEndLimit_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenStartAndEndLimitsAreEqual_ReturnsSuccess(string locale)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
        result.ErrorMessage.ShouldBeEmpty();
    }


    [Theory]
    [InlineData("en-US", 1, "The execution date cannot be in the past relative to the current date.")] // 1 hour ago
    [InlineData("en-GB", 1, "The execution date cannot be in the past relative to the current date.")] 
    [InlineData("es-ES", 1, "La fecha de ejecución no puede estar en el pasado con respecto a la fecha actual.")] 
    public void CalculateNextExecution_WhenExecutionDateTimeIsInPast_ReturnsError(string locale, int hoursBack, string expectedError)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    
    [Theory]
    [InlineData("en-US", 10, null, 5, "The selected execution date is later than the allowed end limit date.")]
    [InlineData("en-US", 2, 5, null, "The selected execution date is earlier than the allowed start limit date.")]
    [InlineData("en-GB", 10, null, 5, "The selected execution date is later than the allowed end limit date.")]
    [InlineData("en-GB", 2, 5, null, "The selected execution date is earlier than the allowed start limit date.")]
    [InlineData("es-ES", 10, null, 5, "La fecha de ejecución seleccionada es posterior a la fecha límite de fin permitida.")]
    [InlineData("es-ES", 2, 5, null, "La fecha de ejecución seleccionada es anterior a la fecha límite de inicio permitida.")]
    public void CalculateNextExecution_WhenExecutionDateTimeIsOutsideLimits_ReturnsError(
            string locale,
            int daysToExecution,
            int? daysToStartLimit,
            int? daysToEndLimit,
            string expectedErrorMessage )
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
            Locale = locale,
            LimitsStartDateLocal = daysToStartLimit.HasValue ? new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero).AddDays(daysToStartLimit.Value) : null,
            LimitsEndDateLocal = daysToEndLimit.HasValue ? new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero).AddDays(daysToEndLimit.Value) : null
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }

    
    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenOnlyStartLimitExistsAndExecutionIsValid_ReturnsSuccess(string locale)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenOnlyEndLimitExistsAndExecutionIsValid_ReturnsSuccess(string locale)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }

    #endregion Execution Limits

    #region Date/Time Range Limits

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenExecutionDateTimeIsWithinLimits_ReturnsSuccess(string locale)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsStartLimit_ReturnsSuccess(string locale)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenExecutionDateTimeEqualsEndLimit_ReturnsSuccess(string locale)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero));
    }

    #endregion Date/Time Range Limits

    #region TimeZone & Localization

    [Theory]
    [InlineData("en-US", "Occurs once", "05-10-2026", "12:00 PM")]
    [InlineData("en-GB", "Occurs once", "10/05/2026", "12:00")]
    [InlineData("es-ES", "Ocurre una vez", "10/05/2026", "12:00")]
    public void CalculateNextExecution_WhenTimeZoneIsApplied_ConvertsToLocalTime(
            string locale,
            string expectedPrefix,
            string expectedDate,
            string expectedTime )
    {
        // Arrange: 10:00 AM UTC. Madrid en Mayo tiene Horario de Verano (UTC+2).
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = "Romance Standard Time",
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        result.NextExecutionTime.Value.Hour.ShouldBe(12); // 10 UTC + 2 horas de diferencia local
        result.NextExecutionTime.Value.Offset.ShouldBe(TimeSpan.FromHours(2));

        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedDate);
        result.Description.ShouldContain(expectedTime);
    }

    #endregion TimeZone & Localization

    #region Description Formatting

    
    [Theory]
    [InlineData("en-US", "Occurs once", "05-15-2026", "starting on 05-10-2026")]
    [InlineData("en-GB", "Occurs once", "15/05/2026", "starting on 10/05/2026")]
    [InlineData("es-ES", "Ocurre una vez", "15/05/2026", "comenzando el 10/05/2026")]
    public void CalculateNextExecution_WhenStartLimitExists_IncludesStartDateInDescription(
            string locale,
            string expectedPrefix,
            string expectedDate,
            string expectedStartLimitPhrase )
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
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedDate);
        result.Description.ShouldContain(expectedStartLimitPhrase);
    }

    
    [Theory]
    [InlineData("en-US", "starting on")]
    [InlineData("en-GB", "starting on")]
    [InlineData("es-ES", "comenzando el")]
    public void CalculateNextExecution_WhenStartLimitDoesNotExist_ExcludesStartDateFromDescription(
            string locale,
            string excludedStartLimitPhrase )
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotContain(excludedStartLimitPhrase);
    }


    [Theory]
    [InlineData("en-US", "4:00 AM", "01-15-2026")]
    [InlineData("en-GB", "04:00", "15/01/2026")]
    [InlineData("es-ES", "04:00", "15/01/2026")]
    public void CalculateNextExecution_WhenTimeZoneIsApplied_DescriptionUsesLocalTime(
            string locale,
            string expectedTimeInDesc,
            string expectedDateInDesc )
    {
        // Arrange: 10:00 UTC en zona horaria CST (UTC-6 en invierno) = 04:00 CST
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Once,
            Occurs = OccursType.Daily,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // El cálculo numérico debe convertirse correctamente a hora CST (10:00 UTC - 6 horas = 04:00 CST)
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.Description.ShouldContain(expectedTimeInDesc);
        result.Description.ShouldContain(expectedDateInDesc);
    }


    #endregion Description Formatting

    #region Logical Consistency

    
    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenRecursEveryIsProvided_IgnoresForOnceSchedule(string locale)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(config.ExecutionDateTimeLocal);
    }


    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("es-ES")]
    public void CalculateNextExecution_WhenDailyFrequencyIsProvided_IgnoresForOnceSchedule(string locale)
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
            Locale = locale,
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