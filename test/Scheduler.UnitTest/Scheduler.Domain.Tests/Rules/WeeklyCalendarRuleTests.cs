using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Tests.Rules;

public class WeeklyCalendarRuleTests
{

    [Fact]
    public void Monday_and_Thursday_are_valid_on_first_week()
    {
        var start = new DateOnly(2020, 1, 1); // Wednesday
        var schedule = new WeeklySchedule(
            EveryWeeks: 2,
            DaysOfWeek: new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday }
        );

        var thursday = new DateOnly(2020, 1, 2); // Thursday

        Assert.True(WeeklyCalendarRule.IsValidDay(thursday, start, schedule));
    }

    [Fact]
    public void Skips_week_when_not_multiple_of_every_weeks()
    {
        var start = new DateOnly(2020, 1, 1);
        var schedule = new WeeklySchedule(
            EveryWeeks: 2,
            DaysOfWeek: new[] { DayOfWeek.Monday }
        );

        var mondayWeek2 = new DateOnly(2020, 1, 6);   // week +1
        var mondayWeek3 = new DateOnly(2020, 1, 13);  // week +2

        Assert.False(WeeklyCalendarRule.IsValidDay(mondayWeek2, start, schedule));
        Assert.True(WeeklyCalendarRule.IsValidDay(mondayWeek3, start, schedule));
    }

}
