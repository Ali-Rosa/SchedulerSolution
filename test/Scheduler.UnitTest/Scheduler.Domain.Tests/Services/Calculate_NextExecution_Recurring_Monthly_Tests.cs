using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Monthly_Tests
{
    private readonly SchedulerService _service;
    private readonly TimeZoneInfo _utcZone = TimeZoneInfo.Utc;

    public Calculate_NextExecution_Recurring_Monthly_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_FirstThursdayEveryThreeMonthsWithHourlyFrequency_ReturnsExpectedSequence()
    {
        // Start date: 01/01/2020. Recurs every 3 months. First Thursday.
        // Daily frequency: Every 1 hour between 3:00 am and 6:00 am.
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Thursday)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // We request 8 executions to see the month jump (January and April)
        // Note: Remember that maxOccurrences = 1 by default in the strategy
        var result = ScheduleEngine.IterateAndCalculate(
            startDate, config, _utcZone, 8,
            (curr, start) => MonthlyCalendarRule.IsValidDay(curr, start, config.RecursEvery, config.Monthly!),
            (next) => "Test"
        );

        // Assert: The exact sequence from the PDF page 3
        var executions = result.NextExecutionTimes.ToList();

        // Enero (Mes 1)
        executions[0].ShouldBe(new DateTimeOffset(2020, 1, 2, 3, 0, 0, TimeSpan.Zero));
        executions[1].ShouldBe(new DateTimeOffset(2020, 1, 2, 4, 0, 0, TimeSpan.Zero));
        executions[2].ShouldBe(new DateTimeOffset(2020, 1, 2, 5, 0, 0, TimeSpan.Zero));
        executions[3].ShouldBe(new DateTimeOffset(2020, 1, 2, 6, 0, 0, TimeSpan.Zero));

        // Abril (Mes 4 -> Salto de 3 meses)
        executions[4].ShouldBe(new DateTimeOffset(2020, 4, 2, 3, 0, 0, TimeSpan.Zero));
        executions[5].ShouldBe(new DateTimeOffset(2020, 4, 2, 4, 0, 0, TimeSpan.Zero));
        executions[6].ShouldBe(new DateTimeOffset(2020, 4, 2, 5, 0, 0, TimeSpan.Zero));
        executions[7].ShouldBe(new DateTimeOffset(2020, 4, 2, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_SecondWeekendDayWithDailyFrequency_ReturnsExpectedSequence()
    {
        // Start date: 01/01/2020. Second Weekend day.
        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1) 
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.WeekendDay)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 1, new TimeOnly(3, 0), new TimeOnly(6, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        var result = ScheduleEngine.IterateAndCalculate(
            startDate
            , config
            , _utcZone
            , 12, // Request 12 to cover January, February, and March
            (curr, start) => MonthlyCalendarRule.IsValidDay(curr, start, config.RecursEvery, config.Monthly!),
            (next) => "Test"
        );

        // Assert: The exact sequence from the PDF page 4
        var executions = result.NextExecutionTimes.ToList();

        // January (The second weekend day is Sunday 05)
        executions[0].ShouldBe(new DateTimeOffset(2020, 1, 5, 3, 0, 0, TimeSpan.Zero));
        executions[3].ShouldBe(new DateTimeOffset(2020, 1, 5, 6, 0, 0, TimeSpan.Zero));

        // February (The second weekend day is Sunday 02)
        executions[4].ShouldBe(new DateTimeOffset(2020, 2, 2, 3, 0, 0, TimeSpan.Zero));
        executions[7].ShouldBe(new DateTimeOffset(2020, 2, 2, 6, 0, 0, TimeSpan.Zero));

        // March (NOTE: Here the first weekend is Sunday 01. The second weekend day is Saturday 07)
        executions[8].ShouldBe(new DateTimeOffset(2020, 3, 7, 3, 0, 0, TimeSpan.Zero));
        executions[11].ShouldBe(new DateTimeOffset(2020, 3, 7, 6, 0, 0, TimeSpan.Zero));
    }
}