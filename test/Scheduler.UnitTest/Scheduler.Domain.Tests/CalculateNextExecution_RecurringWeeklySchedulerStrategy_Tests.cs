using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_RecurringWeeklySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new RecurringWeeklySchedulerStrategy()]);

    #region Specific validations for strategy

    [Fact]
    public void CalculateNextExecution_WhenWeeklyConfigurationIsNull_ReturnsValidationError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Weekly configuration is required for Weekly recurring schedules.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new object[] { new DayOfWeek[0] })]
    public void CalculateNextExecution_WhenWeeklyDaysAreNullOrEmpty_ReturnsValidationError(DayOfWeek[]? daysOfWeek)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = daysOfWeek! },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Weekly configuration requires at least one day.");
    }

    #endregion Specific validations for strategy

    #region OccursOnce Mode

    [Fact]
    public void CalculateNextExecution_WhenOccursOnceAndValidDay_ReturnsSameDayExecution()
    {
        // Arrange: Wednesday at 10 AM, execution at 3 PM same day
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursOnceAndTimeHasPassed_ReturnsNextValidDay()
    {
        // Arrange: Wednesday at 10 PM, execution at 3 PM already passed
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(3); // Friday
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursOnceAndNoValidDayInCurrentWeek_JumpsToNextWeek()
    {
        // Arrange: Friday at 10 PM, pattern only Tuesday/Wednesday, every 1 week
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Wednesday]
            },
            FirstDayOfWeek = DayOfWeek.Monday,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(7); // Tuesday of next week
        result.NextExecutionTime.Value.Hour.ShouldBe(9);
    }



    #endregion OccursOnce Mode

    #region OccursEvery Mode

    [Theory]
    // Stage: Wednesday, January 1, 2020. Days: Mon, Wed, Fri. Hours: 4, 6, 8 AM.
    [InlineData(5, 1, 6)]  // It's 5 AM -> Next is today at 6 AM
    [InlineData(7, 1, 8)]  // It's 7 AM -> Next is today at 8 AM
    [InlineData(22, 3, 4)] // It's 10 PM -> Next is Friday 03 at 4 AM
    public void CalculateNextExecution_WhenOccursEveryWithinSameDay_ReturnsNextInterval(
        int currentHour, int expectedDay, int expectedHour)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
    }

    #endregion OccursEvery Mode

    #region Weekly Pattern (DaysOfWeek)

    [Fact]
    public void CalculateNextExecution_WhenNextValidDayExistsInSameWeek_ReturnsSameWeekExecution()
    {
        // Today Tuesday 05 at 10:00 AM. Days: Friday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(8);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
    }

    [Theory]
    [InlineData(0, 12, 6)]  // Lunes 04 a las 12:00 PM -> Debe caer el miércoles 06 (Misma semana 0)
    [InlineData(4, 12, 25)] // Viernes 08 a las 12:00 PM -> Salta semana 1 y 2 -> Debe caer el lunes 25 (Semana 3)
    public void CalculateNextExecution_WhenMultipleDaysConfigured_ReturnsCorrectNextExecution(int offsetDays, int offsetHours, int expectedDay)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero).AddDays(offsetDays).AddHours(offsetHours),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 3,
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
    }

    #endregion Weekly Pattern (DaysOfWeek)

    #region Week Recurrence (RecursEvery)

    [Fact]
    public void CalculateNextExecution_WhenRecurringEveryWeeks_SkipsWeeksCorrectly()
    {
        // Monday 04 at 00:01. Every 2 weeks, Monday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        // Monday 04 (1 min passed) -> Monday 11 (Week 1, Skip) -> Monday 18 (Week 2)
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(18);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryAndWeeksAreSkipped_JumpsCorrectNumberOfWeeks()
    {
        // Friday, January 3, 2020 at 10 PM. Pattern: Every 2 weeks, Monday/Friday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
            },
            FirstDayOfWeek = DayOfWeek.Monday,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var result = _service.CalculateNextExecution(config);

        // Week 0 ends. Week 1 is skipped. Week 2 starts on Monday 13 at 4 AM.
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(13);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
    }

    #endregion Week Recurrence (RecursEvery)

    #region Anchor & Default Behavior

    [Fact]
    public void CalculateNextExecution_WhenNoDailyFrequency_UsesAnchorTimeAndIgnoresExecutionDateTimeLocal()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), // Monday 04 at 10:00 AM.
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 20, 11, 0, 0, TimeSpan.Zero).AddHours(5), // 03:00 PM  Should be ignored
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"

        };

        var result = _service.CalculateNextExecution(config);

        // Assert: Next Monday (11) at 10:00 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(11);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.Description.ShouldContain("10:00");
    }

    #endregion Anchor & Default Behavior

    #region Limits Handling

    [Fact]
    public void CalculateNextExecution_WhenEndLimitPreventsExecution_ReturnsError()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] }, // Next Friday is 2026-05-08
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Before the next execution
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
    }

    #endregion Limits Handling

    #region Edge Cases & Boundaries

    [Fact]
    public void CalculateNextExecution_WhenCrossingYearBoundary_ReturnsCorrectWeekExecution()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero).AddMinutes(1),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Day.ShouldBe(5);
    }

    [Fact]
    public void CalculateNextExecution_WhenLeapYearOccurs_HandlesWeeklyCalculationCorrectly()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero).AddMinutes(1),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Thursday] },
            LimitsStartDateLocal = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(3);
        result.NextExecutionTime.Value.Day.ShouldBe(7);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryCrossesMidnight_ReturnsNextValidDayExecution()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 23, 0, 0, TimeSpan.Zero), // Wednesday 11:00 PM
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(22, 0), // 10 PM
                EndTime = new TimeOnly(23, 59, 59) // Until end of day
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Thursday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Since it's 11:00 PM, the 11:00 PM task is not included (e > now).
        // The next available is Thursday (day 2) at 10:00 PM.
        var nextExecution = result.NextExecutionTime.Value;
        nextExecution.Day.ShouldBe(2);
        nextExecution.Hour.ShouldBe(22);
    }

    [Fact]
    public void CalculateNextExecution_WhenNoExecutionsWithinLimits_ReturnsError()
    {
        // Arrange: Pattern is Monday/Friday, but we're past end date before any execution
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 8, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
            },
            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2020, 1, 7, 23, 59, 59, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }


    #endregion Edge Cases & Boundaries

    #region Time Units Handling

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryWithMinutesInterval_ReturnsNextValidExecution()
    {
        // Arrange: Wednesday 4:55 AM, pattern every 15 minutes from 4 AM to 5 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 4, 55, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Minutes,
                FrequencyInterval = 15,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(5, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(5);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
    }

    [Fact]
    public void CalculateNextExecution_WhenOccursEveryWithSecondsInterval_ReturnsNextValidExecution()
    {
        // Arrange: Wednesday 4:00:50, pattern every 20 seconds from 4 AM to 4:01 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 4, 0, 50, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Seconds,
                FrequencyInterval = 20,
                StartTime = new TimeOnly(4, 0, 0),
                EndTime = new TimeOnly(4, 1, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(1);
        result.NextExecutionTime.Value.Second.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]  // Every 1 hour: 5 AM is valid
    [InlineData(2)]  // Every 2 hours: 6 AM is valid (4 + 2)
    [InlineData(4)]  // Every 4 hours: 4 AM is valid (start), next would be 8 AM
    public void CalculateNextExecution_WhenOccursEveryWithHourIntervals_ReturnsNextExecutionWithinValidInterval(int hourInterval)
    {
        // Arrange: Wednesday 5 AM, pattern every N hours from 4 AM to 8 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = hourInterval,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Validate that the result is within the expected range and respects the interval
        var nextHour = result.NextExecutionTime.Value.Hour;
        nextHour.ShouldBeGreaterThanOrEqualTo(4);  // Cannot be before start (4 AM)
        nextHour.ShouldBeLessThanOrEqualTo(8);     // Cannot be after end (8 AM)
    }



    #endregion Time Units Handling

    #region Description & Localization

    [Fact]
    public void CalculateNextExecution_WhenLocaleIsDifferent_FormatsDescriptionAccordingly()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "ru-RU", // Russian culture
        };

        var result = _service.CalculateNextExecution(config);

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("2026");
    }

    [Fact]
    public void CalculateNextExecution_WhenTimeZoneIsProvided_ConvertsExecutionCorrectly()
    {
        // Arrange: Different time zone (e.g., America/New_York)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0) // 3 PM in New York
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = "America/New_York",
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
    }

    #endregion Description & Localization

    #region Logical Consistency

    [Theory]
    [InlineData(DayOfWeek.Monday, 18)] // Empieza en Lunes -> El Lunes 11 es Semana 1 (Se salta) -> Cae el Lunes 18 (Semana 2)
    [InlineData(DayOfWeek.Thursday, 11)] // Empieza en Jueves -> El Lunes 11 es Semana 0 (Coincidencia/Hit) -> Cae el Lunes 11
    public void CalculateNextExecution_WhenFirstDayOfWeekChanges_AffectsWeekGrouping(DayOfWeek firstDayOfWeek, int expectedDay)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Jueves 07 de mayo
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
            FirstDayOfWeek = firstDayOfWeek // Parametrizado mediante la teoría
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
    }

    #endregion Logical Consistency

}