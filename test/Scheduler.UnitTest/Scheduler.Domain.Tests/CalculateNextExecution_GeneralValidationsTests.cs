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
    public void CalculateNextExecution_WhenConfigIsNull_ReturnsErrorInEnglishForDefault()
    {
        // Act
        var result = _service.CalculateNextExecution(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The configuration cannot be null.");
    }

    [Theory]
    [InlineData("en-US", "The schedule is disabled.")]
    [InlineData("en-GB", "The schedule is disabled.")]
    [InlineData("es-ES", "La planificación está deshabilitada.")]
    public void CalculateNextExecution_WhenSchedulerIsDisabled_ReturnsLocalizedError(string locale, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith(expectedErrorMessage);
    }

    [Theory]
    [InlineData("en-US", "Not defined schedule type.")]
    [InlineData("en-GB", "Not defined schedule type.")]
    [InlineData("es-ES", "Tipo de planificación no definido.")]
    public void CalculateNextExecution_WhenSchedulerTypeIsInvalid_ReturnsError(string locale, string expectedErrorMessage)
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
    [InlineData("en-US", "Not defined occurs type.")]
    [InlineData("en-GB", "Not defined occurs type.")]
    [InlineData("es-ES", "Tipo de ocurrencia no definido.")]
    public void CalculateNextExecution_WhenOccursTypeIsInvalid_ReturnsError(string locale, string expectedErrorMessage)
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
    [InlineData("en-US", 0, "The Every value must be greater than 0.")]
    [InlineData("en-US", -1, "The Every value must be greater than 0.")]
    [InlineData("en-GB", 0, "The Every value must be greater than 0.")]
    [InlineData("en-GB", -1, "The Every value must be greater than 0.")]
    [InlineData("es-ES", 0, "El valor 'Cada' debe ser mayor que 0.")]
    [InlineData("es-ES", -1, "El valor 'Cada' debe ser mayor que 0.")]
    public void CalculateNextExecution_WhenRecursEveryIsZeroOrLess_ReturnsError(string locale, int invalidValue, string expectedErrorMessage)
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }

    [Theory]
    [InlineData("en-US", "Within the limits, the start date cannot be later than the end date.")]
    [InlineData("en-GB", "Within the limits, the start date cannot be later than the end date.")]
    [InlineData("es-ES", "Dentro de los límites, la fecha de inicio no puede ser posterior a la de fin.")]
    public void CalculateNextExecution_WhenLimitsStartDateIsAfterEndDate_ReturnsError(string locale, string expectedErrorMessage)
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
    [InlineData("en-US", null, "The TimeZoneId is required.")]
    [InlineData("en-US", "", "The TimeZoneId is required.")]
    [InlineData("en-GB", null, "The TimeZoneId is required.")]
    [InlineData("en-GB", "", "The TimeZoneId is required.")]
    [InlineData("es-ES", null, "El TimeZoneId es requerido.")]
    [InlineData("es-ES", "", "El TimeZoneId es requerido.")]
    public void CalculateNextExecution_WhenTimeZoneIdIsNullOrEmpty_ReturnsError(string locale, string? invalidTimeZoneId, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith(expectedErrorMessage);
    }

    [Theory]
    [InlineData("en-US", "Invalid TimeZoneId")]
    [InlineData("en-GB", "Invalid TimeZoneId")]
    [InlineData("es-ES", "TimeZoneId inválido")]
    public void CalculateNextExecution_WhenTimeZoneIdIsInvalid_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldStartWith(expectedErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CalculateNextExecution_WhenLocaleIsNullOrEmpty_ReturnsErrorInEnglishForDefault(string? invalidLocale)
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
    public void CalculateNextExecution_WhenLocaleIsNotSupported_ReturnsErrorInEnglishForDefault()
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

    [Theory]
    [InlineData("en-US", "The provided FirstDayOfWeek is not a valid day of the week.")]
    [InlineData("en-GB", "The provided FirstDayOfWeek is not a valid day of the week.")]
    [InlineData("es-ES", "El FirstDayOfWeek proporcionado no es un día válido de la semana.")]
    public void CalculateNextExecution_WhenFirstDayOfWeekIsInvalid_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }

    [Theory]
    [InlineData("en-US", "Unsupported schedule and occurs combination.")]
    [InlineData("en-GB", "Unsupported schedule and occurs combination.")]
    [InlineData("es-ES", "Combinación de planificación y ocurrencia no soportada.")]
    public void CalculateNextExecution_WhenNoStrategyMatchesConfiguration_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = schedulerService.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }

    [Theory]
    [InlineData("en-US", "Weekly configuration is required for Weekly recurring schedules.")]
    [InlineData("en-GB", "Weekly configuration is required for Weekly recurring schedules.")]
    [InlineData("es-ES", "La configuración semanal es requerida para planificaciones recurrentes semanales.")]
    public void CalculateNextExecution_WhenWeeklyConfigurationIsMissing_ReturnsError(string locale, string expectedErrorMessage)
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
    [InlineData("en-US", "Monthly configuration is required for Monthly recurring schedules.")]
    [InlineData("en-GB", "Monthly configuration is required for Monthly recurring schedules.")]
    [InlineData("es-ES", "La configuración mensual es requerida para planificaciones recurrentes mensuales.")]
    public void CalculateNextExecution_WhenMonthlyConfigurationIsMissing_ReturnsError(string locale, string expectedErrorMessage)
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
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    #endregion General validations

}