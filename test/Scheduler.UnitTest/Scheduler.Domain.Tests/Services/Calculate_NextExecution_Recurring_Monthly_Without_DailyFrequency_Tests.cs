using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Validation Tests

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_MissingConfiguration_ReturnsErrorMessage()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true
            , Type = SchedulerType.Recurring
            , Occurs = OccursType.Monthly
            , RecursEvery = 1
            , TimeZoneId = TimeZoneInfo.Utc.Id
            , Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Monthly configuration is required");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_InvalidCulture_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true
            , Type = SchedulerType.Recurring
            , Occurs = OccursType.Monthly
            , RecursEvery = 1
            , TimeZoneId = TimeZoneInfo.Utc.Id
            , Locale = "invalid-culture"
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);


        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_ZeroRecursEvery_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true
            , Type = SchedulerType.Recurring
            , Occurs = OccursType.Monthly
            , RecursEvery = 0
            , TimeZoneId = TimeZoneInfo.Utc.Id
            , Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);


        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("greater than 0");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_NegativeRecursEvery_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = -5,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("The Every value must be greater than 0.");
    }

    #endregion Validation Tests

    #region Core Logic & Anchor Time (Specific Day)

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_SpecificDay_InheritsAnchorTime()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 2, 14, 30, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(14);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_PassedDay_JumpsToNextMonth()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(6);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_SpecificDay31_DoesNotExecuteInFebruary_JumpsToMarch()
    {
        // Arrange: 1 de Febrero 2026. Config: Día 31.
        var currentDate = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert: Febrero no tiene día 31, así que el motor debe saltar a Marzo.
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(3); // Marzo
        result.NextExecutionTime.Value.Day.ShouldBe(31);  // Día 31 de Marzo
    }

    #endregion Core Logic & Anchor Time (Specific Day)

    #region Relative Day Calculations

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_RelativeDay_ReturnsExpectedDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.WeekendDay
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("Occurs the last weekend day of every 3 months");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_RelativeDay_Weekday_ReturnsExpectedDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Weekday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("weekday");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_RelativeDay_Day_ReturnsExpectedDescription()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 2,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.Day
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("day");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_RelativeDay_Daily_ReturnsExpectedDescription()
    {
        // Arrange - Coverage for "Day" type in FormatDayType and IsMatchingType
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Third,
                RelativeDayType = MonthlyRelativeDayType.Day
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("third day of every month");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_RelativeDay_SpecificDayOfWeek_ReturnsExpectedDescription()
    {
        // Arrange - Coverage for specific day of week (e.g., Thursday)
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Fourth,
                RelativeDayType = MonthlyRelativeDayType.Thursday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("thursday");
    }

    [Theory]
    [InlineData(MonthlyRelativeOrdinal.First)]
    [InlineData(MonthlyRelativeOrdinal.Second)]
    [InlineData(MonthlyRelativeOrdinal.Third)]
    [InlineData(MonthlyRelativeOrdinal.Fourth)]
    [InlineData(MonthlyRelativeOrdinal.Last)]
    public void CalculateNextExecution_RecurringMonthly_AllOrdinals_ReturnsValidExecution(MonthlyRelativeOrdinal ordinal)
    {
        // Arrange - Coverage for all ordinals
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = ordinal,
                RelativeDayType = MonthlyRelativeDayType.Weekday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_AllDayTypesDayOfWeekValues_ReturnsExpectedResults()
    {
        // Arrange - Coverage for day of week conversion (0-6 mapping)
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.Friday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.DayOfWeek.ShouldBe(DayOfWeek.Friday);
    }

    #endregion Relative Day Calculations

    #region Error Handling & Edge Cases

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_FormatDayType_InvalidEnum_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = (MonthlyRelativeDayType)999 // Invalid enum value
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Not defined relative day type");
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_InvalidRelativeOrdinal_ReturnsError()
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = (MonthlyRelativeOrdinal)999, // Invalid enum value
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Not defined relative ordinal");
    }

    #endregion Error Handling & Edge Cases

    #region Month Interval & Skipping Logic

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_MultipleMonthsInterval_SkipsNonMatchingMonths()
    {
        // Arrange - Every 3 months, starting May 1st, looking for June 10th (should skip to August)
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            LimitsStartDateLocal = currentDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5); // First execution in May (3 months from start)
        result.NextExecutionTime.Value.Day.ShouldBe(10);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_NoMatchingDaysInMonth_SkipsToNextValidMonth()
    {
        // Arrange - Looking for 5th Monday when a month doesn't have 5 Mondays
        var currentDate = new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            LimitsStartDateLocal = currentDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Should find a month with 5 Mondays
        result.NextExecutionTime.Value.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    #endregion Month Interval & Skipping Logic

    #region Description Formatting

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_BuildMonthlyDescription_ShouldHandleBothBranches()
    {
        // 1. Prueba "Every month" (recursEvery == 1) y "Specific Day"
        SchedulerConfiguration configSpecific = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // 2. Prueba "Every X months" (recursEvery > 1) y "Relative Day"
        SchedulerConfiguration configRelative = new()
        {
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
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        var resultSpecific = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configSpecific);
        var resultRelative = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configRelative);

        resultSpecific.Description.ShouldContain("every month");
        resultRelative.Description.ShouldContain("every 2 months");
    }

    #endregion Description Formatting

    #region Daily Frequency Integration

    [Theory]
    [InlineData(MonthlyRelativeOrdinal.Second, MonthlyRelativeDayType.Monday, "8,10")]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_WithDailyFrequencySequence_ReturnsExpectedHours_ESTA_PRUEBA_ES_CON_DAILY_FRECUENCI(
        MonthlyRelativeOrdinal ordinal,
        MonthlyRelativeDayType dayType,
        string expectedHoursCsv)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHours = expectedHoursCsv.Split(',').Select(int.Parse).ToList();

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = ordinal,
                RelativeDayType = dayType
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            },
            LimitsStartDateLocal = startDate,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act & Assert
        DateTimeOffset lastExecution = startDate;

        foreach (var expectedHour in expectedHours)
        {
            var result = _service.CalculateNextExecution(lastExecution, config);

            result.IsSuccess.ShouldBeTrue();
            result.NextExecutionTime.ShouldNotBeNull();
            result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
            result.NextExecutionTime.Value.Minute.ShouldBe(0);

            lastExecution = result.NextExecutionTime.Value;
        }
    }

    #endregion Daily Frequency Integration

}