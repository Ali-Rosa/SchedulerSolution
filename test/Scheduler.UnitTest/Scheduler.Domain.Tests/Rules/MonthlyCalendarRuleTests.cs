using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class MonthlyCalendarRuleTests
{
    [Fact]
    public void Specific_Day_Should_Match_Correctly()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var currentDay = new DateOnly(2026, 4, 15); // Three months have passed
        var config = new SchedulerMonthly(true, 15, null, null);

        // Act
        var result = MonthlyCalendarRule.IsValidDay(currentDay, startDate, 3, config);

        // Assert
        result.ShouldBeTrue("It is the correct month (multiple of 3) and the exact day (15).");
    }

    [Fact]
    public void Specific_Day_Should_Cap_At_End_Of_Month()
    {
        // Arrange: We request the 31st, but February (leap year) only has 29.
        var startDate = new DateOnly(2024, 1, 1);
        var currentDay = new DateOnly(2024, 2, 29); // February 29th
        var config = new SchedulerMonthly(true, 31, null, null);

        // Act
        var result = MonthlyCalendarRule.IsValidDay(currentDay, startDate, 1, config);

        // Assert
        result.ShouldBeTrue("The 31st day in February should automatically adjust to the last valid day of the month (29).");
    }

    [Theory]
    // 2026-05 (May) begins Friday
    [InlineData(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Friday, 1)] // First Friday is 01
    [InlineData(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.Monday, 11)] // Second Monday is 11
    [InlineData(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.Day, 31)] // Last day of the month
    [InlineData(SchedulerMonthlyRelativeOrdinal.Third, SchedulerMonthlyRelativeDayType.WeekendDay, 9)] // Third weekend day (Sun 3, Sat 8, Sun 9) -> Note: In May 2026 (Sat 2, Sun 3, Sat 9) -> The third is Saturday 9.
    public void Relative_Days_Should_Be_Calculated_Correctly(SchedulerMonthlyRelativeOrdinal ordinal, SchedulerMonthlyRelativeDayType dayType, int expectedDay)
    {
        // Arrange
        var startDate = new DateOnly(2026, 5, 1);
        var currentDay = new DateOnly(2026, 5, expectedDay);
        var config = new SchedulerMonthly(false, null, ordinal, dayType);

        // Act
        var result = MonthlyCalendarRule.IsValidDay(currentDay, startDate, 1, config);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Non_Matching_Month_Interval_Should_Be_Invalid()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var currentDay = new DateOnly(2026, 2, 1); // Difference of 1 month
        var config = new SchedulerMonthly(true, 1, null, null);

        // Act
        var result = MonthlyCalendarRule.IsValidDay(currentDay, startDate, 3, config); // Every 3 months
        // Assert
        result.ShouldBeFalse("Febrero no es múltiplo de 3 empezando desde Enero.");
    }

    [Theory]
    // Months with different starts to ensure calendar independence
    // Ordinal, Day Type, Year, Month, Expected Day
    [InlineData(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Monday, 2026, 5, 4)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.Thursday, 2026, 5, 14)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Third, SchedulerMonthlyRelativeDayType.Wednesday, 2026, 5, 20)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Fourth, SchedulerMonthlyRelativeDayType.Saturday, 2026, 5, 23)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.Monday, 2026, 5, 25)]
    [InlineData(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Weekday, 2026, 5, 1)] // First weekday is 1
    [InlineData(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.WeekendDay, 2026, 5, 31)] // Sunday 31
    public void Relative_Days_Complex_Combinations(
        SchedulerMonthlyRelativeOrdinal ordinal,
        SchedulerMonthlyRelativeDayType dayType,
        int year, int month, int expectedDay)
    {
        // Arrange
        var startDate = new DateOnly(year, month, 1);
        var expectedDate = new DateOnly(year, month, expectedDay);
        var config = new SchedulerMonthly(false, null, ordinal, dayType);

        // Act
        var result = MonthlyCalendarRule.IsValidDay(expectedDate, startDate, 1, config);

        // Assert
        result.ShouldBeTrue($"Failure in Ordinal: {ordinal}, Day: {dayType}. Expected day {expectedDay}.");
    }


}