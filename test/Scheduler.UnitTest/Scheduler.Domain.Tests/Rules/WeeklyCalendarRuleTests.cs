using Scheduler.Domain.Rules;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class WeeklyCalendarRuleTests
{
    private readonly DayOfWeek[] _mondayThursdayFriday = [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday];

    [Fact]
    public void WeeklyCalendarRule_Days_In_Initial_Week_Should_Be_Valid_When_Pattern_Matches()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1); // Wednesday
        var thursday = new DateOnly(2020, 1, 2); // Thursday of the same week
        int everyWeeks = 2;
        var firstDay = DayOfWeek.Monday;

        // Act
        var result = WeeklyCalendarRule.IsValidDay(thursday, start, _mondayThursdayFriday, everyWeeks, firstDay);

        // Assert
        result.ShouldBeTrue("Days within the same week as the start date (Week 0) should be valid.");
    }

    [Fact]
    public void WeeklyCalendarRule_Pattern_Should_Skip_Weeks_According_To_Recursion_Value()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1); // Wednesday (Week 0)
        int everyWeeks = 2;
        var firstDay = DayOfWeek.Monday;
        var days = new[] { DayOfWeek.Monday };

        var mondayWeek1 = new DateOnly(2020, 1, 6);   // Next Monday (Week 1)
        var mondayWeek2 = new DateOnly(2020, 1, 13);  // Two Mondays later (Week 2)

        // Act & Assert
        WeeklyCalendarRule.IsValidDay(mondayWeek1, start, days, everyWeeks, firstDay).ShouldBeFalse("Week 1 is not a multiple of 2.");
        WeeklyCalendarRule.IsValidDay(mondayWeek2, start, days, everyWeeks, firstDay).ShouldBeTrue("Week 2 is a multiple of 2.");
    }

    [Fact]
    public void WeeklyCalendarRule_FirstDayOfWeek_Should_Determine_Week_Grouping_Boundaries()
    {
        // Start Date: Saturday 02/05/2026
        // Target Date: Sunday 03/05/2026
        var start = new DateOnly(2026, 5, 2); // Saturday
        var target = new DateOnly(2026, 5, 3); // Sunday
        var days = new[] { DayOfWeek.Sunday };
        int everyWeeks = 2;

        // Case A: Monday as the first day (Saturday and Sunday are in the SAME week 0)
        bool resultMondayStart = WeeklyCalendarRule.IsValidDay(target, start, days, everyWeeks, DayOfWeek.Monday);

        // Case B: Sunday as the first day (Sunday 03 is already in the NEXT week 1)
        bool resultSundayStart = WeeklyCalendarRule.IsValidDay(target, start, days, everyWeeks, DayOfWeek.Sunday);

        // Assert
        resultMondayStart.ShouldBeTrue("With Monday start, Sunday belongs to Week 0.");
        resultSundayStart.ShouldBeFalse("With Sunday start, Sunday belongs to Week 1 (which is skipped).");
    }

    [Fact]
    public void WeeklyCalendarRule_Days_Not_In_Selected_List_Should_Be_Invalid()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 1);
        var tuesday = new DateOnly(2020, 1, 7); // Tuesday
        var selectedDays = new[] { DayOfWeek.Monday }; // Only Monday

        // Act
        var result = WeeklyCalendarRule.IsValidDay(tuesday, start, selectedDays, 1, DayOfWeek.Monday);

        // Assert
        result.ShouldBeFalse("Tuesday was not selected in the schedule configuration.");
    }

    [Fact]
    public void WeeklyCalendarRule_Dates_Prior_To_Start_Should_Be_Invalid()
    {
        // Arrange
        var start = new DateOnly(2020, 1, 10);
        var targetPast = new DateOnly(2020, 1, 3);
        var days = new[] { DayOfWeek.Friday };

        // Act
        var result = WeeklyCalendarRule.IsValidDay(targetPast, start, days, 1, DayOfWeek.Monday);

        // Assert
        result.ShouldBeFalse("The rule must reject any date chronologically before the series start.");
    }

    [Fact]
    public void WeeklyCalendarRule_Sunday_As_Start_Of_Week_Should_Calculate_Pattern_Correctly()
    {
        // Sunday is 0 in the Enum.
        var start = new DateOnly(2026, 5, 10); // Sunday (Week 0)
        var nextSunday = new DateOnly(2026, 5, 24); // Two weeks later (Week 2)
        var days = new[] { DayOfWeek.Sunday };

        // Act
        var result = WeeklyCalendarRule.IsValidDay(nextSunday, start, days, 2, DayOfWeek.Sunday);

        // Assert
        result.ShouldBeTrue("The modulo arithmetic should handle Sunday (0) correctly.");
    }

}