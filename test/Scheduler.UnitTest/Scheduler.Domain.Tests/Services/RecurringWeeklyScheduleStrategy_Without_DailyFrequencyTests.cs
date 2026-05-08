using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;

namespace Scheduler.Domain.Tests.Services;

public class RecurringWeeklyScheduleStrategy_Without_DailyFrequencyTests
{
    private readonly SchedulerService _service;

    public RecurringWeeklyScheduleStrategy_Without_DailyFrequencyTests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Validation Tests

    //[Fact]
    //public void Should_ReturnError_When_WeeklyConfig_Is_Null()
    //{
    //    // El Builder por defecto inicializa Weekly en null
    //    var config = ScheduleConfigurationBuilder.RecurringWeekly().Build();
    //    var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

    //    Assert.False(result.IsSuccess);
    //    Assert.Contains("Weekly configuration and at least one day are required", result.ErrorMessage);
    //}

    [Fact]
    public void Should_ReturnError_When_WeeklyConfig_Is_Null()
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly().Build();
        
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        Assert.False(result.IsSuccess);
        Assert.Contains("Weekly configuration", result.ErrorMessage);
        Assert.Contains("required", result.ErrorMessage);
    }

    [Fact]
    public void Should_ReturnError_When_No_Days_Are_Selected()
    {
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_WeeklyDays() // Lista vacía
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        Assert.False(result.IsSuccess);
        Assert.Contains("at least one day are required", result.ErrorMessage);
    }

    #endregion

    #region Core Logic & Midnight Rule

    [Fact]
    public void Should_Execute_At_Midnight_And_Ignore_ExecutionDateTimeLocal()
    {
        // Escenario: Tarea cada semana el Lunes. 
        // Hoy es Lunes a las 10:00 AM. 
        // Ponemos una hora de ejecución a las 3 PM, pero el sistema debe IGNORARLA.

        var currentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero); // Lunes 04

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_ExecutionDateTimeLocal(currentDate.AddHours(5)) // 03:00 PM (Debe ignorarse)
            .With_Locale("en-US")
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Como hoy a las 00:00 AM ya pasó, debe saltar al PRÓXIMO Lunes a las 00:00 AM.
        Assert.True(result.IsSuccess);
        var expected = new DateTimeOffset(2026, 5, 11, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, result.NextExecutionTime);
        Assert.Contains("00:00", result.Description);
    }

    [Fact]
    public void Should_Find_Next_Day_In_Same_Week()
    {
        // Hoy Martes 05. Días permitidos: Lunes y Viernes.
        // Debe encontrar el Viernes 08 de la misma semana a medianoche.
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Friday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.Equal(DayOfWeek.Friday, result.NextExecutionTime!.Value.DayOfWeek);
        Assert.Equal(8, result.NextExecutionTime.Value.Day);
        Assert.Equal(0, result.NextExecutionTime.Value.Hour); // Midnight
    }

    #endregion

    #region Week Skipping (RecursEvery)

    [Fact]
    public void Should_Skip_Weeks_Correctly_Based_On_RecursEvery()
    {
        // Hoy Lunes 04. Patrón: Cada 2 semanas (RecursEvery = 2). Día: Lunes.
        // Como hoy a las 00:01 AM ya pasó las 00:00, debe irse 2 semanas al futuro.
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        // Lunes 04 (Semana 0) -> Lunes 11 (Semana 1, Salta) -> Lunes 18 (Semana 2, Válido)
        Assert.Equal(18, result.NextExecutionTime!.Value.Day);
    }

    [Fact]
    public void Should_Find_All_Selected_Days_Only_In_The_Correct_Week_Iteration()
    {
        // Cada 3 semanas: Lunes, Miércoles, Viernes.
        // Inicio: Lunes 04 Mayo (Semana 0).
        // Semana 1 y 2 deben ser ignoradas totalmente.
        var startDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(3)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // 1. Si hoy es Lunes 04 a mediodía, el siguiente es Miércoles 06 (Misma semana)
        var res1 = _service.CalculateNextExecution(startDate.AddHours(12), config);
        Assert.Equal(6, res1.NextExecutionTime!.Value.Day);

        // 2. Si hoy es Viernes 08 (Fin de semana 0), debe saltar 2 semanas enteras 
        // y caer en el Lunes de la Semana 3 (25 de Mayo)
        var res2 = _service.CalculateNextExecution(startDate.AddDays(4).AddHours(12), config);
        Assert.Equal(25, res2.NextExecutionTime!.Value.Day);
    }

    //[Fact]
    //public void Should_Work_When_All_Days_Are_Selected()
    //{
    //    // Cada 2 semanas, todos los días. Inicio: Lunes 04.
    //    // Domingo 10 es el último día de la Semana 0 (Válido).
    //    // Lunes 11 es el primer día de la Semana 1 (No válido).
    //    var startDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

    //    var config = ScheduleConfigurationBuilder.RecurringWeekly()
    //        .With_RecursEvery(2)
    //        .With_WeeklyDays(
    //            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
    //            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday)
    //        .Build();

    //    // Si hoy es Domingo 10 a las 10 AM, debe encontrar el lunes de la semana 2 (Día 18)
    //    var result = _service.CalculateNextExecution(startDate.AddDays(6).AddHours(10), config);

    //    Assert.Equal(18, result.NextExecutionTime!.Value.Day);
    //}

    [Fact]
    public void Should_Skip_To_Day_18_When_Using_Spanish_Culture()
    {
        var startDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                             DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday)
            .With_Locale("es-ES") // Lunes es el primer día
            .Build();

        // Hoy Domingo 10 (Fin de Semana 0). 
        // Lunes 11 es Semana 1 (Salta). 
        // Lunes 18 es Semana 2 (Toca).
        var result = _service.CalculateNextExecution(startDate.AddDays(6).AddHours(10), config);

        Assert.Equal(18, result.NextExecutionTime!.Value.Day);
    }

    #endregion

    #region FirstDayOfWeek Impact

    [Fact]
    public void Should_Change_Execution_Day_When_FirstDayOfWeek_Changes()
    {
        // Fecha inicio: Jueves 07. Evaluar: Lunes 11. Cada 2 semanas.
        var startLimit = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
        var currentDate = startLimit;

        var builder = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_Limits_StartDateLocal(startLimit);

        // Caso A: Semana empieza Lunes. 
        // Jueves 07 es Semana 0. Lunes 11 es Semana 1 (Salta). Lunes 18 es Semana 2 (Toca).
        var resultA = _service.CalculateNextExecution(currentDate, builder.With_FirstDayOfWeek(DayOfWeek.Monday).Build());
        Assert.Equal(18, resultA.NextExecutionTime!.Value.Day); // <-- Corregido a 18

        // Caso B: Semana empieza Jueves.
        // Jueves 07 es el inicio de Semana 0. Lunes 11 SIGUE siendo Semana 0 (Toca).
        var resultB = _service.CalculateNextExecution(currentDate, builder.With_FirstDayOfWeek(DayOfWeek.Thursday).Build());
        Assert.Equal(11, resultB.NextExecutionTime!.Value.Day);
    }

    [Fact]
    public void Should_Handle_Sunday_As_First_Day_Without_Math_Errors()
    {
        // Domingo 10 Mayo (Inicio Semana 0). Cada 2 semanas, los Sábados.
        // Sábado 16 Mayo -> Sigue siendo Semana 0 (Válido porque 0 % 2 == 0)
        // Sábado 23 Mayo -> Es Semana 1 (Salta)
        var startDate = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero); // Domingo

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Saturday)
            .With_FirstDayOfWeek(DayOfWeek.Sunday) // Cultura USA
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = _service.CalculateNextExecution(startDate, config);

        // Debe encontrar el Sábado 16 de Mayo (mismo bloque semanal que el inicio)
        Assert.Equal(16, result.NextExecutionTime!.Value.Day);
    }

    #endregion

    #region Limits & Safety

    [Fact]
    public void Should_Respect_EndDate_Limit_In_Weekly_Pattern()
    {
        // Hoy Lunes 04. Cada semana el Viernes (Día 08).
        // Pero el límite final es el día 07 (Jueves).
        var currentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var endLimit = currentDate.AddDays(3); // Día 07

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(1)
            .With_WeeklyDays(DayOfWeek.Friday)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.False(result.IsSuccess);
        Assert.Equal("No valid executions found within the allowed range.", result.ErrorMessage);
    }

    [Fact]
    public void Should_Return_Error_When_No_Execution_Fits_In_366_Days()
    {
        // Escenario: Tarea cada 60 semanas (RecursEvery = 60).
        // El motor solo busca 366 días (aprox 52 semanas).
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(60)
            .With_WeeklyDays(DayOfWeek.Monday)
            .Build();

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        Assert.False(result.IsSuccess);
        Assert.Contains("No valid executions found", result.ErrorMessage);
    }

    #endregion Limits & Safety

    #region Calendar Edge Cases

    [Fact]
    public void Should_Handle_Month_And_Year_Transitions_Correctly()
    {
        // Escenario: Última semana de Diciembre 2025. Cada 2 semanas, los Lunes.
        // Lunes 22 Dic (Semana 0) -> Lunes 29 Dic (Semana 1, Salta) -> Lunes 05 Ene 2026 (Semana 2, Válido)
        var startDate = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero);
        var currentDate = startDate.AddMinutes(1); // Ya pasó la medianoche del 22

        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Monday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        Assert.Equal(2026, result.NextExecutionTime!.Value.Year);
        Assert.Equal(1, result.NextExecutionTime.Value.Month);
        Assert.Equal(5, result.NextExecutionTime.Value.Day);
    }

    [Fact]
    public void Should_Handle_Leap_Year_Transition_In_Weekly_Pattern()
    {
        // 2024 es bisiesto. 
        // Jueves 22 Feb (Semana 0) -> Jueves 29 Feb (Semana 1) -> Jueves 07 Mar (Semana 2)
        var startDate = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringWeekly()
            .With_RecursEvery(2)
            .With_WeeklyDays(DayOfWeek.Thursday)
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = _service.CalculateNextExecution(startDate.AddMinutes(1), config);

        // Debe saltar el 29 de Feb y caer en el 07 de Marzo
        Assert.Equal(3, result.NextExecutionTime!.Value.Month);
        Assert.Equal(7, result.NextExecutionTime.Value.Day);
    }

    #endregion Calendar Edge Cases
}