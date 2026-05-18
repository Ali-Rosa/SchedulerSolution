using Scheduler.Domain.Rules;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class DailyCalendarRuleTests
{
    [Fact]
    public void DailyCalendarRule_Start_Day_Should_Always_Be_Valid()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var today = new DateOnly(2026, 5, 1);
        int every = 3;

        // Act
        var result = DailyCalendarRule.IsValidDay(today, start, every);

        // Assert
        result.ShouldBeTrue("The same start date must always be valid (diff = 0).");
    }

    [Fact]
    public void DailyCalendarRule_Day_That_Matches_Recursion_Pattern_Should_Be_Valid()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var target = new DateOnly(2026, 5, 7); // 6 days difference
        int every = 3; // 6 % 3 == 0

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DailyCalendarRule_Day_That_Does_Not_Match_Recursion_Pattern_Should_Be_Invalid()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var target = new DateOnly(2026, 5, 3); // 2 days difference
        int every = 3; // 2 % 3 != 0

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DailyCalendarRule_Past_Dates_Relative_To_Start_Should_Be_Invalid()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 10);
        var past = new DateOnly(2026, 5, 9);
        int every = 1;

        // Act
        var result = DailyCalendarRule.IsValidDay(past, start, every);

        // Assert
        result.ShouldBeFalse("The rule should never validate dates chronologically prior to the start date.");
    }

    [Fact]
    public void DailyCalendarRule_Zero_RecursEvery_Should_Only_Allow_The_Start_Day()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var nextDay = new DateOnly(2026, 5, 2);

        // Act & Assert
        DailyCalendarRule.IsValidDay(start, start, 0).ShouldBeTrue();
        DailyCalendarRule.IsValidDay(nextDay, start, 0).ShouldBeFalse();
    }

    [Fact]
    public void DailyCalendarRule_Leap_Year_Transitions_Should_Be_Calculated_Correctly()
    {
        // Arrange: 2024 is a leap year (Feb 29 exists)
        var start = new DateOnly(2024, 2, 28);
        var target = new DateOnly(2024, 3, 1); // 2 days later
        int every = 2;

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        result.ShouldBeTrue("The calculation must account for February 29th in leap years.");
    }

    [Fact]
    public void DailyCalendarRule_Long_Distance_Dates_Should_Maintain_Pattern_Integrity()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1);
        var target = start.AddDays(300); // exactly 300 days later
        int every = 100;

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        result.ShouldBeTrue();
    }

}