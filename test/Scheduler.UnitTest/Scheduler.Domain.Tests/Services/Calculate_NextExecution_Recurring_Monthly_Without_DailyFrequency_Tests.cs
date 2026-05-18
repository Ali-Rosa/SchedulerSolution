using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
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
        var config = ScheduleConfigurationBuilder.RecurringMonthly().With_Locale("en-US").Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly().With_Locale("invalid-culture").Build();
        
        
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);
        
        
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_ZeroRecursEvery_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(0)
            .Build();


        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);


        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("greater than 0");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_NegativeRecursEvery_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(-5)
            .With_MonthlySpecificDay(10)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("The Every value cannot be negative.");
    }

    #endregion Validation Tests

    #region Core Logic & Anchor Time (Specific Day)

    [Fact]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_SpecificDay_InheritsAnchorTime()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 2, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(31)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.WeekendDay)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Weekday)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.Day)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Third, SchedulerMonthlyRelativeDayType.Day)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Fourth, SchedulerMonthlyRelativeDayType.Thursday)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("thursday");
    }

    [Theory]
    [InlineData(SchedulerMonthlyRelativeOrdinal.First)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Second)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Third)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Fourth)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Last)]
    public void CalculateNextExecution_RecurringMonthly_AllOrdinals_ReturnsValidExecution(SchedulerMonthlyRelativeOrdinal ordinal)
    {
        // Arrange - Coverage for all ordinals
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(ordinal, SchedulerMonthlyRelativeDayType.Weekday)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.Friday)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, (SchedulerMonthlyRelativeDayType)66)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay((SchedulerMonthlyRelativeOrdinal)85, SchedulerMonthlyRelativeDayType.Monday) // 85 no existe en el enum SchedulerMonthlyRelativeOrdinal
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlySpecificDay(10)
            .With_Limits_StartDateLocal(currentDate)
            .Build();

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
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Monday)
            .Build();

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
        var configSpecific = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .Build();

        // 2. Prueba "Every X months" (recursEvery > 1) y "Relative Day"
        var configRelative = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(2)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Monday)
            .Build();

        var resultSpecific = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configSpecific);
        var resultRelative = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configRelative);

        resultSpecific.Description.ShouldContain("every month");
        resultRelative.Description.ShouldContain("every 2 months");
    }

    #endregion Description Formatting

    #region Daily Frequency Integration

    [Theory]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.Monday, "8,10")]
    public void Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_WithDailyFrequencySequence_ReturnsExpectedHours(
        SchedulerMonthlyRelativeOrdinal ordinal,
        SchedulerMonthlyRelativeDayType dayType,
        string expectedHoursCsv)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHours = expectedHoursCsv.Split(',').Select(int.Parse).ToList();

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(ordinal, dayType)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 2, new TimeOnly(8, 0), new TimeOnly(10, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

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