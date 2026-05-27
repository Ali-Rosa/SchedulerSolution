using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service = new([new RecurringMonthlySchedulerStrategy()]);

    #region Monthly Relative Day Scenarios

    [Theory]
    // Paso 1: Consulta inicial (01/01/2020 00:00) -> Cae el 1º Jueves a la 1ª hora (02/01/2020 03:00)
    [InlineData(2020, 1, 1, 0, 2020, 1, 2, 3)]
    // Paso 2: Ya se ejecutó a las 3:00, consulta en ese instante -> Cae el mismo día a las 04:00 (2ª hora)
    [InlineData(2020, 1, 2, 3, 2020, 1, 2, 4)]
    // Paso 3: Consulta a las 04:00 -> Cae el mismo día a las 05:00 (3ª hora)
    [InlineData(2020, 1, 2, 4, 2020, 1, 2, 5)]
    // Paso 4: Consulta a las 05:00 -> Cae el mismo día a las 06:00 (última hora del día)
    [InlineData(2020, 1, 2, 5, 2020, 1, 2, 6)]
    // Paso 5: El día se agotó (consulta a las 06:00) -> Salta 3 meses hasta ABRIL (02/04/2020 03:00)
    [InlineData(2020, 1, 2, 6, 2020, 4, 2, 3)]
    public void CalculateNextExecution_RecurringMonthly_WithHourlyDailyFrequency_ShouldReturnExpectedSequence(
        int queryYear,
        int queryMonth,
        int queryDay,
        int queryHour,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedHour)
    {
        // Arrange
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Construimos las fechas de consulta y de aserción dinámicamente según la teoría
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Thursday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(3, 0),
                EndTime = new TimeOnly(6, 0)
            },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/01/2020 00:00) -> Cae el Domingo 05/01 (2º fin de semana) a las 03:00
    [InlineData(2020, 1, 1, 0, 2020, 1, 5, 3)]
    // Paso 2: Consulta el mismo Domingo 05/01 a las 05:00 -> Cae el mismo día a la última hora (06:00)
    [InlineData(2020, 1, 5, 5, 2020, 1, 5, 6)]
    // Paso 3: Día de Enero agotado (consulta a las 06:00) -> Salta a Febrero (Domingo 02/02 a las 03:00)
    [InlineData(2020, 1, 5, 6, 2020, 2, 2, 3)]
    // Paso 4: Día de Febrero agotado (consulta a las 06:00) -> Salta a Marzo (Sábado 07/03 a las 03:00)
    [InlineData(2020, 2, 2, 6, 2020, 3, 7, 3)]
    public void CalculateNextExecution_RecurringMonthly_SecondWeekendDayWithHourlyDailyFrequency_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Construimos las fechas de consulta y aserción dinámicamente según el caso de prueba
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.WeekendDay
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(3, 0),
                EndTime = new TimeOnly(6, 0)
            },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    #endregion Monthly Relative Day Scenarios

    #region Occurs Once with Monthly Patterns

    [Theory]
    [InlineData(50)]   // Caso 1: Excede el límite máximo (50 > 31)
    [InlineData(0)]    // Caso 2: Por debajo del límite mínimo (0 < 1)
    [InlineData(null)] // Caso 3: Valor nulo / ausente
    public void CalculateNextExecution_RecurringMonthly_InvalidSpecificDayNumber_ReturnsValidationError(int? specificDayNumber)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = specificDayNumber // Parametrizado mediante la teoría
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The day must be between 1 and 31.");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnceDaily_SpecificDay_ReturnsExpectedTime()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: May 15 at 14:30
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(15);
        result.NextExecutionTime.Value.Hour.ShouldBe(14);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnceDaily_LastWeekday_ReturnsExpectedTime()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.Weekday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(17, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Last weekday of May at 17:00
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Hour.ShouldBe(17);
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Saturday);
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Sunday);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/01/2026 00:00) -> Cae el mismo 01 de Enero a las 09:00 AM
    [InlineData(2026, 1, 1, 0, 2026, 1, 1, 9)]
    // Paso 2: Consulta el 01 de Enero a las 09:00 AM -> Salta 3 meses -> Cae el 01 de Abril a las 09:00 AM
    [InlineData(2026, 1, 1, 9, 2026, 4, 1, 9)]
    // Paso 3: Consulta el 01 de Abril a las 09:00 AM -> Salta 3 meses -> Cae el 01 de Julio a las 09:00 AM
    [InlineData(2026, 4, 1, 9, 2026, 7, 1, 9)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDayWithOnceDailyFrequency_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Construimos las fechas de consulta y aserción de forma dinámica según el caso de prueba
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 1
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    #endregion Occurs Once with Monthly Patterns

    #region Occurs Every (Minutes/Hours/Minutes) with Monthly Patterns

    [Theory]
    // Paso 1: Consulta inicial (01/05/2026 10:00) -> Cae el 10 de Mayo a la primera hora (08:00 AM)
    [InlineData(2026, 5, 1, 10, 0, 2026, 5, 10, 8, 0)]
    // Paso 2: Consulta el 10 de Mayo a las 08:00 AM -> Cae el mismo día a las 08:15 AM
    [InlineData(2026, 5, 10, 8, 0, 2026, 5, 10, 8, 15)]
    // Paso 3: Consulta el 10 de Mayo a las 08:15 AM -> Cae el mismo día a las 08:30 AM
    [InlineData(2026, 5, 10, 8, 15, 2026, 5, 10, 8, 30)]
    // Paso 4: Consulta el 10 de Mayo a las 08:30 AM -> Cae el mismo día a las 08:45 AM
    [InlineData(2026, 5, 10, 8, 30, 2026, 5, 10, 8, 45)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDayWithMinutesDailyFrequency_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour, int queryMinute,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        // Construimos las fechas de consulta y aserción dinámicamente incluyendo horas y minutos según la teoría
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, queryMinute, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Minutes,
                FrequencyInterval = 15,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(9, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/05/2026 10:00) -> Segundo lunes de Mayo (11/05/2026 a las 06:00)
    [InlineData(2026, 5, 1, 10, 2026, 5, 11, 6)]
    // Paso 2: Consulta el lunes 11/05 a las 06:00 -> Cae el mismo día a las 12:00 (mediodía)
    [InlineData(2026, 5, 11, 6, 2026, 5, 11, 12)]
    // Paso 3: Consulta el lunes 11/05 a las 12:00 -> Cae el mismo día a las 18:00 (tarde)
    [InlineData(2026, 5, 11, 12, 2026, 5, 11, 18)]
    // Paso 4: Día de Mayo agotado (consulta a las 18:00) -> Salta al segundo lunes de Junio (08/06/2026 a las 06:00)
    [InlineData(2026, 5, 11, 18, 2026, 6, 8, 6)]
    public void CalculateNextExecution_RecurringMonthly_RelativeDayWithHourlyDailyFrequency_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        // Corrección: Declaramos explícitamente startDate al inicio del bloque
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado de forma limpia en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 6,
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(18, 0)
            },
            LimitsStartDateLocal = startDate, // Ahora compila de forma segura
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/05/2026 10:00:00) -> Cae el 5 de Mayo a la primera hora (10:00:00 AM)
    [InlineData(2026, 5, 1, 10, 0, 0, 2026, 5, 5, 10, 0, 0)]
    // Paso 2: Consulta el 5 de Mayo a las 10:00:00 AM -> Cae el mismo día a las 10:00:30 AM
    [InlineData(2026, 5, 5, 10, 0, 0, 2026, 5, 5, 10, 0, 30)]
    // Paso 3: Consulta el 5 de Mayo a las 10:00:30 AM -> Cae el mismo día a las 10:01:00 AM
    [InlineData(2026, 5, 5, 10, 0, 30, 2026, 5, 5, 10, 1, 0)]
    // Paso 4: Consulta el 5 de Mayo a las 10:01:00 AM -> Cae el mismo día a las 10:01:30 AM
    [InlineData(2026, 5, 5, 10, 1, 0, 2026, 5, 5, 10, 1, 30)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDayWithSecondsDailyFrequency_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour, int queryMinute, int querySecond,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Construimos las fechas de consulta y aserción dinámicamente incluyendo horas, minutos y segundos según la teoría
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, queryMinute, querySecond, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 5
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Seconds,
                FrequencyInterval = 30,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 2)
            },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    #endregion Occurs Every (Minutes/Hours/Minutes) with Monthly Patterns

    #region Edge Cases with Daily Frequency

    [Theory]
    // Paso 1: Consulta inicial (15/02/2026 10:00) -> Salta a Marzo (31/03/2026 a las 17:00)
    [InlineData(2026, 2, 15, 10, 2026, 3, 31, 17)]
    // Paso 2: Consulta el 31/03 a las 17:00 -> Cae el mismo día a las 18:00
    [InlineData(2026, 3, 31, 17, 2026, 3, 31, 18)]
    // Paso 3: Consulta el 31/03 a las 18:00 -> Cae el mismo día a las 19:00 (última hora del día)
    [InlineData(2026, 3, 31, 18, 2026, 3, 31, 19)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDay31_JumpsToFirstAvailableMonth_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(19, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Theory]
    // Paso 1: Consulta inicial (15/02/2026 10:00) -> Salta a Mayo (31/05/2026 a las 17:00)
    [InlineData(2026, 2, 15, 10, 2026, 5, 31, 17)]
    // Paso 2: Consulta el 31/05 a las 17:00 -> Cae el mismo día a las 18:00
    [InlineData(2026, 5, 31, 17, 2026, 5, 31, 18)]
    // Paso 3: Consulta el 31/05 a las 18:00 -> Cae el mismo día a las 19:00 (última hora del día)
    [InlineData(2026, 5, 31, 18, 2026, 5, 31, 19)]
    public void CalculateNextExecution_RecurringMonthly_RecursEvery3MonthsSpecificDay31_JumpsToFirstAvailableMonth_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3, // Cada 3 meses
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(19, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/01/2026 00:00) -> Cae el 15 de Enero a las 08:00 AM
    [InlineData(2026, 1, 1, 0, 2026, 1, 15, 8)]
    // Paso 2: Consulta el 15/01 a las 08:00 -> Cae el mismo día a las 12:00 (mediodía)
    [InlineData(2026, 1, 15, 8, 2026, 1, 15, 12)]
    // Paso 3: Consulta el 15/01 a las 12:00 -> Cae el mismo día a las 16:00 (tarde)
    [InlineData(2026, 1, 15, 12, 2026, 1, 15, 16)]
    // Paso 4: Consulta el 15/01 a las 16:00 -> Cae el mismo día a las 20:00 (última del día)
    [InlineData(2026, 1, 15, 16, 2026, 1, 15, 20)]
    // Paso 5: El día de Enero se agotó (consulta a las 20:00) -> Salta 2 meses -> Cae el 15 de Marzo a las 08:00 AM
    [InlineData(2026, 1, 15, 20, 2026, 3, 15, 8)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDayWithEveryTwoMonthsInterval_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 2, // Cada 2 meses
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 4,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(20, 0)
            },
            LimitsStartDateLocal = startDate, // Corregido: startDate declarada correctamente en Arrange
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_TimeInPast_SkipsToNextMonth()
    {
        // Arrange: Request on May 15 at 16:00, looking for 10:00 execution on day 15
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 16, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Should skip to June 15 at 10:00
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime!.Value.Month.ShouldBe(6);
        result.NextExecutionTime!.Value.Day.ShouldBe(15);
        result.NextExecutionTime!.Value.Hour.ShouldBe(10);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/05/2026 10:00:00) -> Cae el 20 de Mayo a la primera hora (10:00:00 AM)
    [InlineData(2026, 5, 1, 10, 0, 0, 2026, 5, 20, 10, 0, 0)]
    // Paso 2: Consulta el 20 de Mayo a las 10:00:00 AM -> Cae el mismo día a las 10:00:15 AM
    [InlineData(2026, 5, 20, 10, 0, 0, 2026, 5, 20, 10, 0, 15)]
    // Paso 3: Consulta el 20 de Mayo a las 10:00:15 AM -> Cae el mismo día a las 10:00:30 AM
    [InlineData(2026, 5, 20, 10, 0, 15, 2026, 5, 20, 10, 0, 30)]
    // Paso 4: Consulta el 20 de Mayo a las 10:00:30 AM -> Cae el mismo día a las 10:00:45 AM
    [InlineData(2026, 5, 20, 10, 0, 30, 2026, 5, 20, 10, 0, 45)]
    public void CalculateNextExecution_RecurringMonthly_SpecificDayWithSecondsDailyFrequencyWithinSameMinute_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour, int queryMinute, int querySecond,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        // Arrange
        // Construimos las fechas de consulta y aserción dinámicamente incluyendo horas, minutos y segundos según la teoría
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, queryMinute, querySecond, TimeSpan.Zero), // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 20
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Seconds,
                FrequencyInterval = 15,
                StartTime = new TimeOnly(10, 0, 0),
                EndTime = new TimeOnly(10, 0, 45)
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, TimeSpan.Zero));
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Day29_OnlyExecutesInLeapYearFebruary()
    {
        // Arrange: Today is January 30, 2025 (non-leap year). Configuration: 29th of each month.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 1, 30, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 29
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: February 2025 does not have a 29th. March 2025 does have a 29th.
        // It should skip February and find March 29th.
        result.NextExecutionTime!.Value.Month.ShouldBe(3);
        result.NextExecutionTime!.Value.Day.ShouldBe(29);
    }

    [Theory]
    // Paso 1: Consulta inicial (01/04/2026 00:00) -> Último fin de semana de Abril (Domingo 26/04 a las 00:00)
    [InlineData(2026, 4, 1, 0, 2026, 4, 26, 0)]
    // Paso 2: Consulta el lunes 27/04 a las 00:00 -> Salta a Mayo (Domingo 31/05 a las 00:00)
    [InlineData(2026, 4, 27, 0, 2026, 5, 31, 0)]
    public void CalculateNextExecution_RecurringMonthly_LastWeekendDayVariesCorrectlyByMonthLength_ShouldReturnExpectedSequence(
            int queryYear, int queryMonth, int queryDay, int queryHour,
            int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        // Construimos las fechas de consulta y aserción dinámicamente según la teoría
        var currentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero);
        var expectedExecution = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            CurrentDate = currentDate, // Integrado correctamente en el config
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.WeekendDay
            },
            LimitsStartDateLocal = startDate, // Corregido: declaramos e inicializamos startDate en el Arrange
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(expectedExecution);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_SpecificDayPassedTime_JumpsToNextMonth()
    {
        // Arrange: Today is May 10 at 11:00. The task is on the 10th at 10:00.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 10, 11, 0, 0, TimeSpan.Zero), // Since 10:00 has already passed, it should go to June 10.
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime!.Value.Month.ShouldBe(6); // June
        result.NextExecutionTime!.Value.Day.ShouldBe(10);
        result.NextExecutionTime!.Value.Hour.ShouldBe(10);
    }

    #endregion Edge Cases with Daily Frequency

    #region TimeZone & Localization with Daily Frequency

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEvery_DifferentTimeZone_ReturnsCorrectLocalTime()
    {
        // Arrange: UTC time with CST timezone conversion
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0)
            },
            TimeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Should convert to local time
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Offset.TotalHours.ShouldBeGreaterThan(-6);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_DescriptionIncludesTimeZoneInfo_REVISAR()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "es-ES",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Description should include time information
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("14:30");
    }

    #endregion TimeZone & Localization with Daily Frequency

    #region Description & Validation with Daily Frequency

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEvery_ReturnsCorrectDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 3,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Description should include frequency details
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("every 3 hours");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_ReturnsCorrectDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 2,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Description should describe the pattern
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("first Monday");
    }

    #endregion Description & Validation with Daily Frequency

}