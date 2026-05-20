using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Monthly Relative Day Scenarios

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_FirstThursdayEveryThreeMonthsWithHourlyFrequency_ReturnsExpectedExecutionSequence()
    {
        // Arrange (PDF Page 2 & 3)
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Thursday)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
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
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_SecondWeekendDayEveryMonthWithHourlyFrequency_ReturnsExpectedExecutionSequence()
    {
        // Arrange (PDF Page 4)
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1) // Cada mes
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.Second, MonthlyRelativeDayType.WeekendDay)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
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

    #endregion Monthly Relative Day Scenarios

    #region Daily Frequency Precedence

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnceIsEnabled_TakesPrecedenceOverOccursEvery_ReturnsCorrectTime_YA_NO_EXISTE()
    {
        // Arrange
        var frequencyAmbigua = new ScheduleDailyFrequency(
            OnceTime: new TimeOnly(18, 0),
            OccursEveryEnable: false, IntervalUnit: TimeIntervalUnit.Hours, FrequencyInterval: 2,
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

    #endregion Daily Frequency Precedence

    #region Occurs Once with Monthly Patterns

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnceDaily_SpecificDay_ReturnsExpectedTime()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(15)
            .With_DailyFrequency_OccursOnce(new TimeOnly(14, 30))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.Last, MonthlyRelativeDayType.Weekday)
            .With_DailyFrequency_OccursOnce(new TimeOnly(17, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Last weekday of May at 17:00
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Hour.ShouldBe(17);
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Saturday);
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnceDaily_EveryThreeMonths_ReturnsExpectedSequence()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlySpecificDay(1)
            .With_DailyFrequency_OccursOnce(new TimeOnly(9, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert
        var exec1 = _service.CalculateNextExecution(startDate, config);
        exec1.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));

        var exec2 = _service.CalculateNextExecution(exec1.NextExecutionTime!.Value, config);
        exec2.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero)); // 3 months later

        var exec3 = _service.CalculateNextExecution(exec2.NextExecutionTime!.Value, config);
        exec3.NextExecutionTime.ShouldBe(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero)); // 3 more months
    }

    #endregion Occurs Once with Monthly Patterns

    #region Occurs Every (Minutes/Hours/Minutes) with Monthly Patterns

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEveryMinutes_SpecificDay_ReturnsExpectedSequence()
    {
        // Arrange: Every 15 minutes from 8:00 to 9:00 on the 10th of each month
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Minutes, 15, new TimeOnly(8, 0), new TimeOnly(9, 0))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);
        var result4 = _service.CalculateNextExecution(result3.NextExecutionTime!.Value, config);

        // Assert: 8:00, 8:15, 8:30, 8:45 on May 10
        result1.NextExecutionTime!.Value.Minute.ShouldBe(0);
        result2.NextExecutionTime!.Value.Minute.ShouldBe(15);
        result3.NextExecutionTime!.Value.Minute.ShouldBe(30);
        result4.NextExecutionTime!.Value.Minute.ShouldBe(45);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEveryHours_RelativeDay_ReturnsExpectedSequence()
    {
        // Arrange: Every 6 hours from 6:00 to 18:00 on the second Monday
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.Second, MonthlyRelativeDayType.Monday)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 6, new TimeOnly(6, 0), new TimeOnly(18, 0))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);
        var result4 = _service.CalculateNextExecution(result3.NextExecutionTime!.Value, config);

        // Assert: 6:00, 12:00, 18:00 on the second Monday, then next month
        result1.NextExecutionTime!.Value.Hour.ShouldBe(6);
        result2.NextExecutionTime!.Value.Hour.ShouldBe(12);
        result3.NextExecutionTime!.Value.Hour.ShouldBe(18);
        result4.NextExecutionTime!.Value.Month.ShouldBe(6); // Next month
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEverySeconds_SpecificDay_ReturnsExpectedSequence()
    {
        // Arrange: Every 30 seconds from 10:00 to 10:02 on the 5th
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(5)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Seconds, 30, new TimeOnly(10, 0), new TimeOnly(10, 2))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);
        var result4 = _service.CalculateNextExecution(result3.NextExecutionTime!.Value, config);

        // Assert: 10:00:00, 10:00:30, 10:01:00, 10:01:30 on May 5
        result1.NextExecutionTime!.Value.Second.ShouldBe(0);
        result2.NextExecutionTime!.Value.Second.ShouldBe(30);
        result3.NextExecutionTime!.Value.Second.ShouldBe(0);
        result3.NextExecutionTime!.Value.Minute.ShouldBe(1);
    }

    #endregion Occurs Every (Minutes/Hours/Minutes) with Monthly Patterns

    #region Edge Cases with Daily Frequency

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_SpecificDay31_JumpsToFirstAvailableMonth()
    {
        // Arrange: Hoy es 15 de Febrero de 2026. Buscamos el día 31.
        var currentDate = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(31)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(17, 0), new TimeOnly(19, 0))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);

        // Assert: El primer mes disponible con día 31 es MARZO (mes 3)
        result1.NextExecutionTime!.Value.Month.ShouldBe(3); // Marzo
        result1.NextExecutionTime!.Value.Day.ShouldBe(31);
        result1.NextExecutionTime!.Value.Hour.ShouldBe(17);

        result2.NextExecutionTime!.Value.Month.ShouldBe(3);
        result2.NextExecutionTime!.Value.Day.ShouldBe(31);
        result2.NextExecutionTime!.Value.Hour.ShouldBe(18);

        result3.NextExecutionTime!.Value.Month.ShouldBe(3);
        result3.NextExecutionTime!.Value.Day.ShouldBe(31);
        result3.NextExecutionTime!.Value.Hour.ShouldBe(19);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_RecursEvery3MonthsSpecificDay31_JumpsToMay()
    {
        // Arrange: Hoy 15 de Feb 2026. Buscamos el día 31 cada 3 meses.
        var currentDate = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3) // Cada 3 meses
            .With_MonthlySpecificDay(31)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(17, 0), new TimeOnly(19, 0))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);

        // Assert: Debe saltar hasta Mayo 2026
        result1.NextExecutionTime!.Value.Month.ShouldBe(5); // Mayo
        result1.NextExecutionTime!.Value.Day.ShouldBe(31);
        result1.NextExecutionTime!.Value.Hour.ShouldBe(17);

        result2.NextExecutionTime!.Value.Month.ShouldBe(5);
        result2.NextExecutionTime!.Value.Day.ShouldBe(31);
        result2.NextExecutionTime!.Value.Hour.ShouldBe(18);

        result3.NextExecutionTime!.Value.Month.ShouldBe(5);
        result3.NextExecutionTime!.Value.Day.ShouldBe(31);
        result3.NextExecutionTime!.Value.Hour.ShouldBe(19);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEvery_EveryTwoMonths_WithDailyFrequency_ReturnsExpectedSequence()
    {
        // Arrange: Every 2 months, every 4 hours from 8:00 to 20:00 on the 15th
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_MonthlySpecificDay(15)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 4, new TimeOnly(8, 0), new TimeOnly(20, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act
        var exec1 = _service.CalculateNextExecution(startDate, config);
        var exec2 = _service.CalculateNextExecution(exec1.NextExecutionTime!.Value, config);
        var exec3 = _service.CalculateNextExecution(exec2.NextExecutionTime!.Value, config);
        var exec4 = _service.CalculateNextExecution(exec3.NextExecutionTime!.Value, config);
        var exec5 = _service.CalculateNextExecution(exec4.NextExecutionTime!.Value, config); // Jump to next 2-month cycle

        // Assert: Jan 15 (8:00, 12:00, 16:00, 20:00), then March 15 (8:00)
        exec1.NextExecutionTime!.Value.Month.ShouldBe(1);
        exec1.NextExecutionTime!.Value.Day.ShouldBe(15);
        exec1.NextExecutionTime!.Value.Hour.ShouldBe(8);

        exec2.NextExecutionTime!.Value.Month.ShouldBe(1);
        exec2.NextExecutionTime!.Value.Hour.ShouldBe(12);

        exec3.NextExecutionTime!.Value.Month.ShouldBe(1);
        exec3.NextExecutionTime!.Value.Hour.ShouldBe(16);

        exec4.NextExecutionTime!.Value.Month.ShouldBe(1);
        exec4.NextExecutionTime!.Value.Hour.ShouldBe(20);

        exec5.NextExecutionTime!.Value.Month.ShouldBe(3); // Salta a marzo (2 meses después de enero)
        exec5.NextExecutionTime!.Value.Day.ShouldBe(15);
        exec5.NextExecutionTime!.Value.Hour.ShouldBe(8);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_TimeInPast_SkipsToNextMonth()
    {
        // Arrange: Request on May 15 at 16:00, looking for 10:00 execution on day 15
        var currentDate = new DateTimeOffset(2026, 5, 15, 16, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(15)
            .With_DailyFrequency_OccursOnce(new TimeOnly(10, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Should skip to June 15 at 10:00
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime!.Value.Month.ShouldBe(6);
        result.NextExecutionTime!.Value.Day.ShouldBe(15);
        result.NextExecutionTime!.Value.Hour.ShouldBe(10);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursEvery_WithinSameMinute_ReturnsExpectedResults()
    {
        // Arrange: Every 15 seconds from 10:00:00 to 10:00:45 on day 20
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(20)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Seconds, 15, new TimeOnly(10, 0, 0), new TimeOnly(10, 0, 45))
            .Build();

        // Act
        var result1 = _service.CalculateNextExecution(currentDate, config);
        var result2 = _service.CalculateNextExecution(result1.NextExecutionTime!.Value, config);
        var result3 = _service.CalculateNextExecution(result2.NextExecutionTime!.Value, config);
        var result4 = _service.CalculateNextExecution(result3.NextExecutionTime!.Value, config);

        // Assert: 10:00:00, 10:00:15, 10:00:30, 10:00:45
        result1.NextExecutionTime!.Value.Second.ShouldBe(0);
        result2.NextExecutionTime!.Value.Second.ShouldBe(15);
        result3.NextExecutionTime!.Value.Second.ShouldBe(30);
        result4.NextExecutionTime!.Value.Second.ShouldBe(45);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_Day29_OnlyExecutesInLeapYearFebruary()
    {
        // Arrange: Today is January 30, 2025 (non-leap year). Configuration: 29th of each month.
        var currentDate = new DateTimeOffset(2025, 1, 30, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(29)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: February 2025 does not have a 29th. March 2025 does have a 29th.
        // It should skip February and find March 29th.
        result.NextExecutionTime!.Value.Month.ShouldBe(3);
        result.NextExecutionTime!.Value.Day.ShouldBe(29);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_LastWeekendDay_VariesCorrectlyByMonthLength()
    {
        // Arrange: "Last weekend day" each month.
        var startDate = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.Last, MonthlyRelativeDayType.WeekendDay)
            .Build();

        // Act
        // April 2026 has 30 days. The last weekend day is the 26th (Sunday).
        var resultApril = _service.CalculateNextExecution(startDate, config);

        // May 2026 has 31 days. The last weekend day is the 31st (Sunday).
        var resultMay = _service.CalculateNextExecution(resultApril.NextExecutionTime!.Value.AddDays(1), config);

        // Assert
        resultApril.NextExecutionTime!.Value.Day.ShouldBe(26);
        resultMay.NextExecutionTime!.Value.Day.ShouldBe(31);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_SpecificDayPassedTime_JumpsToNextMonth()
    {
        // Arrange: Today is May 10 at 11:00. The task is on the 10th at 10:00.
        // Since 10:00 has already passed, it should go to June 10.
        var currentDate = new DateTimeOffset(2026, 5, 10, 11, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .With_DailyFrequency_OccursOnce(new TimeOnly(10, 0)) // Task at 10am
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_TimeZoneId(cstZone.Id)
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(8, 0), new TimeOnly(12, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Should convert to local time
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Offset.TotalHours.ShouldBeGreaterThan(-6);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_DescriptionIncludesTimeZoneInfo()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("es-ES")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .With_DailyFrequency_OccursOnce(new TimeOnly(14, 30))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(15)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 3, new TimeOnly(9, 0), new TimeOnly(18, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert: Description should include frequency details
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("every 3 hours");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_OccursOnce_ReturnsCorrectDescription()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Monday)
            .With_DailyFrequency_OccursOnce(new TimeOnly(15, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert: Description should describe the pattern
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("first Monday");
    }

    #endregion Description & Validation with Daily Frequency

    #region cases from task three

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_FirstThursdayEvery3Months_ReturnsSequence()
    {
        // Arrange: Start date 01/01/2020. Every 3 months. First Thursday.
        // Daily: Every 1 hour from 03:00 to 06:00.
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Thursday)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert
        // El primer jueves de enero es 02/01/2020.

        // Enero: Hora 3, 4, 5, 6
        var r1 = _service.CalculateNextExecution(startDate, config);
        r1.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 3, 0, 0, TimeSpan.Zero));

        var r2 = _service.CalculateNextExecution(r1.NextExecutionTime!.Value, config);
        r2.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 4, 0, 0, TimeSpan.Zero));

        var r3 = _service.CalculateNextExecution(r2.NextExecutionTime!.Value, config);
        r3.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.Zero));

        var r4 = _service.CalculateNextExecution(r3.NextExecutionTime!.Value, config);
        r4.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 2, 6, 0, 0, TimeSpan.Zero));

        // Salto a Abril (3 meses después): Primer jueves de Abril es 02/04/2020
        var r5 = _service.CalculateNextExecution(r4.NextExecutionTime!.Value, config);
        r5.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 4, 2, 3, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_With_DailyFrequency_SecondWeekendDayEveryMonth_ReturnsSequence()
    {
        // Arrange: Start date 01/01/2020. Every 1 month. Second Weekend Day.
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(MonthlyRelativeOrdinal.Second, MonthlyRelativeDayType.WeekendDay)
            .With_DailyFrequency_OccursEvery(TimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert

        // ENERO: Segundo fin de semana (Domingo 05). Horas: 3, 4, 5, 6
        var r1 = _service.CalculateNextExecution(startDate, config);
        r1.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 5, 3, 0, 0, TimeSpan.Zero));

        // Skip to hour 6 (simulando avance de tiempo)
        var r4 = _service.CalculateNextExecution(r1.NextExecutionTime!.Value.AddHours(2), config);
        r4.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 1, 5, 6, 0, 0, TimeSpan.Zero));

        // FEBRERO: Segundo fin de semana (Domingo 02).
        var r5 = _service.CalculateNextExecution(r4.NextExecutionTime!.Value, config);
        r5.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 2, 2, 3, 0, 0, TimeSpan.Zero));

        // MARZO: (Dom 01 es 1er finde, Sab 07 es 2do).
        var r9 = _service.CalculateNextExecution(r5.NextExecutionTime!.Value.AddHours(3), config);
        r9.NextExecutionTime.ShouldBe(new DateTimeOffset(2020, 3, 7, 3, 0, 0, TimeSpan.Zero));
    }

    #endregion cases from task three

}