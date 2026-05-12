using Scheduler.Domain.Rules;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class ScheduleEngineTests
{
    private readonly TimeZoneInfo _utcZone = TimeZoneInfo.Utc;

    #region Iteration Limit Tests

    [Fact]
    public void Should_Stop_At_MaxOccurrences_Even_If_Iterations_Available()
    {
        // Arrange: Request 5 occurrences, iterate daily for 366 days
        var currentDate = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            5,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: Only 5 results, even though we could iterate for 366 days
        result.IsSuccess.ShouldBeTrue();
        result.NextsExecutionsTimes.Count().ShouldBe(5);
    }

    [Fact]
    public void Should_Stop_At_366_Day_Limit()
    {
        // Arrange: Request 500 occurrences (impossible in 366 iterations), daily every day
        var currentDate = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            500,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: Limited to 366 iterations at most
        result.IsSuccess.ShouldBeTrue();
        result.NextsExecutionsTimes.Count().ShouldBeLessThanOrEqualTo(366);
    }

    #endregion Iteration Limit Tests

    #region Date Limit Enforcement

    [Fact]
    public void Should_Respect_LimitsStartDateLocal()
    {
        // Arrange: Start limit is in the future
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            1,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: First result should be >= startLimit
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Date.ShouldBeGreaterThanOrEqualTo(startLimit.Date);
    }

    [Fact]
    public void Should_Respect_LimitsEndDateLocal()
    {
        // Arrange: End limit in the near future
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var endLimit = new DateTimeOffset(2026, 5, 10, 23, 59, 59, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            10,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: All results should be <= endLimit
        result.IsSuccess.ShouldBeTrue();
        foreach (var execution in result.NextsExecutionsTimes)
        {
            execution.ShouldBeLessThanOrEqualTo(endLimit);
        }
    }

    [Fact]
    public void Should_Respect_Both_StartAndEnd_Limits()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var endLimit = new DateTimeOffset(2026, 5, 15, 23, 59, 59, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            20,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();
        foreach (var execution in result.NextsExecutionsTimes)
        {
            execution.ShouldBeGreaterThanOrEqualTo(startLimit);
            execution.ShouldBeLessThanOrEqualTo(endLimit);
        }
    }

    #endregion Date Limit Enforcement

    #region Logic Validation

    [Fact]
    public void IsDayValidLogic_False_Should_Skip_Days()
    {
        // Arrange: Only allow even-numbered days
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            3,
            (currentDay, startDay) => currentDay.Day % 2 == 0, // Only even days
            (nextDate) => "Test"
        );

        // Assert: Should find May 2, 4, 6 (all even days)
        result.IsSuccess.ShouldBeTrue();
        result.NextsExecutionsTimes.Count().ShouldBe(3);
        result.NextsExecutionsTimes.ElementAt(0).Day.ShouldBe(2);
        result.NextsExecutionsTimes.ElementAt(1).Day.ShouldBe(4);
        result.NextsExecutionsTimes.ElementAt(2).Day.ShouldBe(6);
    }

    [Fact]
    public void Results_Should_Be_Sorted_Chronologically()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            10,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var executions = result.NextsExecutionsTimes.ToList();
        for (int i = 0; i < executions.Count - 1; i++)
        {
            executions[i].ShouldBeLessThan(executions[i + 1]);
        }
    }

    #endregion Logic Validation

    #region Description Generation

    [Fact]
    public void BuildDescriptionLogic_Should_Be_Called_With_First_Execution()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .Build();

        var capturedDate = DateTimeOffset.MinValue;

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            3,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => {
                capturedDate = nextDate;
                return "Description for " + nextDate.Date;
            }
        );

        // Assert: BuildDescriptionLogic should receive the first execution
        result.IsSuccess.ShouldBeTrue();
        capturedDate.ShouldBe(result.NextsExecutionsTimes.First());
        result.Description.ShouldContain(result.NextsExecutionsTimes.First().Date.ToString());
    }

    #endregion Description Generation

    #region Edge Cases

    [Fact]
    public void CurrentDate_Greater_Than_StartLimit_Should_Use_CurrentDate()
    {
        // Arrange: Current date is after the start limit
        var currentDate = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            1,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: Should start from currentDate (not startLimit)
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Date.ShouldBeGreaterThanOrEqualTo(currentDate.Date);
    }

    [Fact]
    public void CurrentDate_Less_Than_StartLimit_Should_Use_StartLimit()
    {
        // Arrange: Current date is before the start limit
        var currentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = ScheduleEngine.IterateAndCalculate(
            currentDate,
            config,
            _utcZone,
            1,
            (currentDay, startDay) => DailyCalendarRule.IsValidDay(currentDay, startDay, 1),
            (nextDate) => "Test"
        );

        // Assert: Should start from startLimit
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Date.ShouldBeGreaterThanOrEqualTo(startLimit.Date);
    }

    #endregion Edge Cases

}