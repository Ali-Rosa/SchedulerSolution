using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Tests.Rules;

public class DailyCalendarRuleTests
{
    [Fact]
    public void Same_Day_Is_Always_Valid_If_Not_Past()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var today = new DateOnly(2026, 5, 1);
        int every = 3;

        // Act
        var result = DailyCalendarRule.IsValidDay(today, start, every);

        // Assert
        Assert.True(result, "The same start date must always be valid (diff = 0)");
    }

    [Fact]
    public void Valid_When_Difference_Is_Multiple_Of_EveryDays()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var target = new DateOnly(2026, 5, 7); // 6 days difference
        int every = 3; // 6 % 3 == 0

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Invalid_When_Difference_Is_Not_Multiple_Of_EveryDays()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 1);
        var target = new DateOnly(2026, 5, 3); // 2 days difference
        int every = 3; // 2 % 3 != 0

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Invalid_For_Past_Dates()
    {
        // Arrange
        var start = new DateOnly(2026, 5, 10);
        var past = new DateOnly(2026, 5, 9);
        int every = 1;

        // Act
        var result = DailyCalendarRule.IsValidDay(past, start, every);

        // Assert
        Assert.False(result, "Should not allow dates before the start date");
    }

    [Fact]
    public void EveryDays_Zero_Only_Valid_On_StartDay()
    {
        // Special scenario: If every is 0, only the start day is valid
        var start = new DateOnly(2026, 5, 1);
        var target = new DateOnly(2026, 5, 2);

        Assert.True(DailyCalendarRule.IsValidDay(start, start, 0));
        Assert.False(DailyCalendarRule.IsValidDay(target, start, 0));
    }

    [Fact]
    public void Works_Across_Month_Boundaries_And_Leap_Years()
    {
        // Test the transition from February to March in a leap year
        var start = new DateOnly(2024, 2, 28); // 2024 is a leap year
        var target = new DateOnly(2024, 3, 1); // 2 days later (28th and 29th of Feb)
        int every = 2;

        var result = DailyCalendarRule.IsValidDay(target, start, every);

        Assert.True(result, "Should correctly calculate days including February 29th");
    }

    [Fact]
    public void Large_Gap_Test()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1);
        var target = start.AddDays(300); // 300 days later
        int every = 100;

        // Act
        var result = DailyCalendarRule.IsValidDay(target, start, every);

        // Assert
        Assert.True(result, "300 is a multiple of 100");
    }
}