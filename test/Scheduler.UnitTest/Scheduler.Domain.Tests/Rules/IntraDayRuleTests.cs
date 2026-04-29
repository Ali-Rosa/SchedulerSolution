using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Tests.Rules;

public class IntraDayRuleTests
{
    [Fact]
    public void Generates_executions_every_2_hours_within_range()
    {
        // Arrange
        var day = new DateOnly(2020, 1, 1);

        var schedule = new IntraDaySchedule(
            Unit: IntraDayFrequencyUnit.Hours,
            Every: 2,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(8, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        // Act
        var executions = IntraDayRule .GetExecutionsForDay(day, schedule, timeZone) .ToList();

        // Assert
        Assert.Equal(3, executions.Count);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 4, 0, 0, TimeSpan.Zero), executions[0]);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 6, 0, 0, TimeSpan.Zero), executions[1]);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 8, 0, 0, TimeSpan.Zero), executions[2]);
    }

    [Fact]
    public void Generates_single_execution_when_start_equals_end()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new IntraDaySchedule(
            Unit: IntraDayFrequencyUnit.Hours,
            Every: 1,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(10, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        var executions = IntraDayRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .ToList();

        Assert.Single(executions);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero), executions[0]);
    }

    [Fact]
    public void Does_not_generate_execution_after_end_time()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new IntraDaySchedule(
            Unit: IntraDayFrequencyUnit.Hours,
            Every: 3,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(8, 0)
        );

        var timeZone = TimeZoneInfo.Utc;

        var executions = IntraDayRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .ToList();

        Assert.Equal(2, executions.Count);
        Assert.Equal(new TimeOnly(4, 0), TimeOnly.FromDateTime(executions[0].UtcDateTime));
        Assert.Equal(new TimeOnly(7, 0), TimeOnly.FromDateTime(executions[1].UtcDateTime));
    }

    [Fact]
    public void Converts_local_times_to_utc_correctly()
    {
        var day = new DateOnly(2020, 1, 1);

        var schedule = new IntraDaySchedule(
            Unit: IntraDayFrequencyUnit.Hours,
            Every: 2,
            StartTime: new TimeOnly(4, 0),
            EndTime: new TimeOnly(4, 0)
        );

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        var execution = IntraDayRule
            .GetExecutionsForDay(day, schedule, timeZone)
            .Single();

        // 04:00 CET = 03:00 UTC in winter
        Assert.Equal(new TimeOnly(3, 0), TimeOnly.FromDateTime(execution.UtcDateTime));
    }


}






