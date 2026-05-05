using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;


namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_RecurringWeeklyScheduleStrategyTest
{
    private readonly SchedulerService _service;
    public CalculateNextExecution_RecurringWeeklyScheduleStrategyTest() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Weekly_Every2Weeks_MonThuFri_WithIntraDay_ReturnsCorrectSequence()
    {
        // ARRANGE
        // Miércoles 01/01/2020 (Semana 1)
        var referenceDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder
            .RecurringDaily()
            .With_Limits_StartDateLocal(referenceDate)
            .WithWeekly(2, DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday)
            .With_DailyFrecuency(false, new TimeOnly(0, 0), true, TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();

        // ACT & ASSERT

        // 1. Si hoy es Miércoles 01/01 y son las 00:00 -> Próxima es Jueves 02/01 a las 04:00
        var now = referenceDate;
        var result = _service.CalculateNextExecution(now, config);
        Assert.Equal(new DateTimeOffset(2020, 1, 2, 4, 0, 0, TimeSpan.Zero), result.NextExecutionTime);

        // 2. Si ya son las 04:00 del Jueves -> Próxima es las 06:00 del mismo día
        now = result.NextExecutionTime!.Value;
        result = _service.CalculateNextExecution(now, config);
        Assert.Equal(new DateTimeOffset(2020, 1, 2, 6, 0, 0, TimeSpan.Zero), result.NextExecutionTime);

        // 3. Si son las 08:00 del Jueves (límite final del día) -> Próxima es Viernes 03/01 a las 04:00
        now = new DateTimeOffset(2020, 1, 2, 8, 0, 0, TimeSpan.Zero);
        result = _service.CalculateNextExecution(now, config);
        Assert.Equal(new DateTimeOffset(2020, 1, 3, 4, 0, 0, TimeSpan.Zero), result.NextExecutionTime);

        // 4. Salto de semana: Después del Viernes 03/01 a las 08:00, 
        // toca esperar 2 semanas hasta el Lunes 13/01 a las 04:00
        now = new DateTimeOffset(2020, 1, 3, 8, 0, 1, TimeSpan.Zero);
        result = _service.CalculateNextExecution(now, config);
        Assert.Equal(new DateTimeOffset(2020, 1, 13, 4, 0, 0, TimeSpan.Zero), result.NextExecutionTime);
    }
}
