using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Tests.Rules;

public class WeeklyCalendarRuleTests
{
    private readonly DayOfWeek[] _mondayThursdayFriday = [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday];

    [Fact]
    public void Days_In_Same_Week_Of_Start_Are_Valid_With_Every_2_Weeks()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1); // Wednesday
        var thursday = new DateOnly(2020, 1, 2); // Thursday of the same week
        int everyWeeks = 2;
        var firstDay = DayOfWeek.Monday;

        // Act
        var result = WeeklyCalendarRule.IsValidDay(thursday, start, _mondayThursdayFriday, everyWeeks, firstDay);

        // Assert
        Assert.True(result, "Thursday should be valid because it is in week 0 relative to Wednesday");
    }

    [Fact]
    public void Skips_Week_Correctly_When_Every_2_Weeks()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1); // Wednesday (Week 0)
        int everyWeeks = 2;
        var firstDay = DayOfWeek.Monday;
        var days = new[] { DayOfWeek.Monday };

        var mondayWeek1 = new DateOnly(2020, 1, 6);   // Next Monday (Week 1)
        var mondayWeek2 = new DateOnly(2020, 1, 13);  // Two Mondays later (Week 2)

        // Act & Assert
        Assert.False(WeeklyCalendarRule.IsValidDay(mondayWeek1, start, days, everyWeeks, firstDay), "Should be false: week 1 is not a multiple of 2");
        Assert.True(WeeklyCalendarRule.IsValidDay(mondayWeek2, start, days, everyWeeks, firstDay), "Should be true: week 2 is a multiple of 2");
    }

    [Fact]
    public void FirstDayOfWeek_Changes_Week_Grouping_Crucially()
    {
        // SCENARIO:
        // Start Date: Saturday 02/05/2026
        // Target Date: Sunday 03/05/2026
        // If the week starts on MONDAY: Saturday and Sunday are in the SAME week (Week 0).
        // If the week starts on SUNDAY: Sunday is already in the NEXT week (Week 1).
        var start = new DateOnly(2026, 5, 2); // Saturday
        var target = new DateOnly(2026, 5, 3); // Sunday
        var days = new[] { DayOfWeek.Sunday };
        int everyWeeks = 2;

        // Case A: Monday as the first day
        bool resultMonday = WeeklyCalendarRule.IsValidDay(target, start, days, everyWeeks, DayOfWeek.Monday);

        // Case B: Sunday as the first day
        bool resultSunday = WeeklyCalendarRule.IsValidDay(target, start, days, everyWeeks, DayOfWeek.Sunday);

        // Assert
        Assert.True(resultMonday, "With Monday as the start, Sunday is Week 0 (Valid)");
        Assert.False(resultSunday, "With Sunday as the start, Sunday is Week 1 (Invalid for 'Every 2 weeks')");
    }

    [Fact]
    public void Returns_False_If_Day_Is_Not_In_Selected_Days()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1);
        var tuesday = new DateOnly(2020, 1, 7); // Tuesday
        var days = new[] { DayOfWeek.Monday }; // Only Monday

        // Act
        var result = WeeklyCalendarRule.IsValidDay(tuesday, start, days, 1, DayOfWeek.Monday);

        // Assert
        Assert.False(result, "Should be false because Tuesday is not in the list of selected days");
    }

    [Fact]
    public void Returns_False_If_Target_Day_Is_Before_Start_Date()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 10);
        var targetPast = new DateOnly(2020, 1, 3); // One week before
        var days = new[] { DayOfWeek.Friday };

        // Act
        var result = WeeklyCalendarRule.IsValidDay(targetPast, start, days, 1, DayOfWeek.Monday);

        // Assert
        Assert.False(result, "Should not allow dates before the start date");
    }

    [Fact]
    public void Handles_Sunday_As_First_Day_Correctly()
    {
        // Sunday is 0 in the Enum. Let's verify that the formula (current - first + 7) % 7 does not fail.
        var start = new DateOnly(2026, 5, 10); // Sunday (Week 0)
        var nextSunday = new DateOnly(2026, 5, 24); // Two weeks later (Week 2)
        var days = new[] { DayOfWeek.Sunday };

        var result = WeeklyCalendarRule.IsValidDay(nextSunday, start, days, 2, DayOfWeek.Sunday);

        Assert.True(result);
    }
}