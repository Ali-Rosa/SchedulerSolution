using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_RecurringDailySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new RecurringDailySchedulerStrategy()]);

    #region Mode Selection (Once vs Every)

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryIsDisabled_OnceTimeIsEnabledAndIgnoresIntervalConfiguration()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    #endregion Mode Selection (Once vs Every)

    #region Mode: Occurs Once
    
    [Fact]
    public void CalculateNextExecution_WhenOnceModeAndTimeIsInFuture_ReturnsSameDayExecution()
    {
        // Arrange: 10 AM now, execution at 3 PM today.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    [Fact]
    public void CalculateNextExecution_WhenOnceModeAndTimeHasPassed_ReturnsNextValidDay()
    {
        // Arrange: 10 PM now, execution was at 8 AM. Should be tomorrow.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(8, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(7);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
    }

    #endregion Mode: Occurs Once

    #region Mode: Occurs Every

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryAndNextIntervalExists_ReturnsNextInterval()
    {
        // Arrange: 5 AM now. Hours: 4, 6, 8 AM. Next: 6 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryAndDayIsExhausted_ReturnsNextPatternDay()
    {
        // Arrange: 10 PM now. Pattern every 3 days. Hours 4-8 AM. Next: Day 09 at 4 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 3,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(9);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
    }

    #endregion Mode: Occurs Every

    #region Calendar Pattern (RecursEvery / Days)

    [Fact]
    public void CalculateNextExecution_WhenRecurringEveryDays_SkipsDaysCorrectly()
    {
        // Arrange: Day 01 + every 3 days = Day 04
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 3,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(4);
        result.NextExecutionTime.Value.Hour.ShouldBe(12);
    }

    #endregion Calendar Pattern (RecursEvery / Days)

    #region Anchor & Default Behavior

    [Fact]
    public void CalculateNextExecution_WhenNoDailyFrequency_UsesCurrentTimeAsAnchorAndMovesToNextDay()
    {
        // Arrange: Request at 10:30 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 10:30 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
        result.Description.ShouldContain("10:30");
    }

    #endregion Anchor & Default Behavior

    #region Limits Handling

    [Theory]
    [InlineData(TimeIntervalUnit.Minutes, 15, 12, 15)]
    [InlineData(TimeIntervalUnit.Seconds, 20, 12, 0, 20)]
    public void CalculateNextExecution_WhenOccursEveryWithSmallUnits_ReturnsCorrectExecution(TimeIntervalUnit unit, int interval, int h, int m, int s = 0)
    {
        // Arrange: 12:00:00 exact.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = unit,
                FrequencyInterval = interval,
                StartTime = new TimeOnly(12, 0, 0),
                EndTime = new TimeOnly(13, 0, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(h);
        result.NextExecutionTime.Value.Minute.ShouldBe(m);
        result.NextExecutionTime.Value.Second.ShouldBe(s);
    }

    [Fact]
    public void CalculateNextExecution_WhenStartLimitIsInFuture_AdjustsExecutionCorrectly()
    {
        // Arrange: Request on day 01 at 10 AM. Limit on day 10.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero).AddDays(9),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    [Fact]
    public void CalculateNextExecution_WhenEndLimitPreventsExecution_ReturnsError()
    {
        // Arrange: Today is day 01. Every 10 days (next is day 11). Limit is day 05.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 10,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero).AddDays(4),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
    }


    #endregion Limits Handling  

    #region Edge Cases & Boundaries

    [Fact]
    public void CalculateNextExecution_WhenCurrentTimeEqualsStartTime_ReturnsNextInterval()
    {
        // Scenario: If it's exactly 04:00:00, the filter 'e > now' should jump to 06:00.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 4, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
    }

    [Fact]
    public void CalculateNextExecution_WhenCrossingYearBoundary_ReturnsNextDayCorrectly()
    {
        // Arrange: 31 Dec -> 1 Jan
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
    }

    [Fact]
    public void CalculateNextExecution_WhenLeapYearOccurs_HandlesFebruary29Correctly()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(2);
        result.NextExecutionTime.Value.Day.ShouldBe(29);
    }

    #endregion Edge Cases & Boundaries

    #region Time Units Handling
    #endregion Time Units Handling

    #region Description & Localization

    [Fact]
    public void CalculateNextExecution_WhenOccursEvery_GeneratesDetailedDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.Description.ShouldContain("Every 2 hours");
        result.Description.ShouldContain("at 04:00");
    }

    [Theory]
    [InlineData(1, "Occurs every day")]
    [InlineData(3, "Occurs every 3 days")]
    public void CalculateNextExecution_WhenRecurringPattern_DescriptionReflectsInterval(int every, string expectedPrefix)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = every,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.Description.ShouldContain(expectedPrefix);
    }

    #endregion Description & Localization

    #region Logical Consistency

    [Fact]
    public void CalculateNextExecution_WhenNoDailyFrequency_IgnoresExecutionDateTimeLocal()
    {
        // Arrange: 8 AM request, 11 PM configured (but should be ignored)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero).AddHours(15), // 11 PM, but should be ignored
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 8 AM
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
    }

    [Fact]
    public void CalculateNextExecution_WhenFirstDayOfWeekChanges_DoesNotAffectDailyCalculation()
    {
        // Arrange: The daily strategy should not change if the week starts on Monday or Sunday
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var resMonday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Monday });
        var resSunday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Sunday });

        // Assert
        resMonday.NextExecutionTime.ShouldBe(resSunday.NextExecutionTime);
    }

    #endregion Logical Consistency

}