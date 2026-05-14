using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_FirstThursdayEveryThreeMonthsWithHourlyFrequency_ReturnsExpectedExecutionSequence()
    {
        // Arrange (PDF Page 2 & 3)
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Thursday)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert (Simulate successive requests to the service)

        // 1. Request from the start (Start Date)
        var exec1 = _service.CalculateNextExecution(startDate, config);
        exec1.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 3, 0, 0, TimeSpan.Zero));

        // 2. Simulate that it has already executed, request the next one (same day, hour 4)
        var exec2 = _service.CalculateNextExecution(exec1.NextExecutionTime!.Value, config);
        exec2.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 4, 0, 0, TimeSpan.Zero));

        // 3. Hour 5
        var exec3 = _service.CalculateNextExecution(exec2.NextExecutionTime!.Value, config);
        exec3.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.Zero));

        // 4. Hour 6 (End of the day)
        var exec4 = _service.CalculateNextExecution(exec3.NextExecutionTime!.Value, config);
        exec4.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 6, 0, 0, TimeSpan.Zero));

        // 5. The day is exhausted. The next execution should jump 3 months to APRIL.
        var exec5 = _service.CalculateNextExecution(exec4.NextExecutionTime!.Value, config);
        exec5.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 4, 2, 3, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_SecondWeekendDayEveryMonthWithHourlyFrequency_ReturnsExpectedExecutionSequence()
    {
        // Arrange (PDF Page 4)
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1) // Cada mes
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.WeekendDay)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert

        // JANUARY: Second weekend day is Sunday 05
        var execJanStart = _service.CalculateNextExecution(startDate, config);
        execJanStart.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 5, 3, 0, 0, TimeSpan.Zero));

        var execJanEnd = _service.CalculateNextExecution(execJanStart.NextExecutionTime!.Value.AddHours(2), config); // Skip to hour 6
        execJanEnd.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 5, 6, 0, 0, TimeSpan.Zero));

        // FEBRUARY: Second weekend day is Sunday 02
        var execFebStart = _service.CalculateNextExecution(execJanEnd.NextExecutionTime!.Value, config);
        execFebStart.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 2, 2, 3, 0, 0, TimeSpan.Zero));

        // MARCH: (Sunday 01 is the 1st weekend day, Saturday 07 is the 2nd).
        // We advance to request the jump directly to March.
        var execMarStart = _service.CalculateNextExecution(execFebStart.NextExecutionTime!.Value.AddHours(3), config);
        execMarStart.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 3, 7, 3, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_OccursOnceIsEnabled_TakesPrecedenceOverOccursEvery_ReturnsCorrectTime()
    {
        // Arrange
        var frequencyAmbigua = new ScheduleDailyFrequency(
            OccursOnceEnable: true, OnceTime: new TimeOnly(18, 0),
            OccursEveryEnable: true, IntervalUnit: SchedulerTimeIntervalUnit.Hours, FrequencyInterval: 2,
            StartTime: new TimeOnly(4, 0), EndTime: new TimeOnly(8, 0)
        );

        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_MonthlySpecificDay(10)
            .With_DailyFrequency(frequencyAmbigua)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Day 10 at 18:00 (Ignores the OccursEvery from 4 to 8)
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(18);
    }
}