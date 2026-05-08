using System.Globalization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Scheduler.Domain.Tests.TestHelpers.Builders;

namespace Scheduler.Domain.Tests.Rules;

public class DescriptionRuleTests
{
    private readonly TimeZoneInfo _cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"); // UTC-6

    [Fact]
    public void Format_Basic_Description_Correctly()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero); // 10:00 UTC
        var config = ScheduleConfigurationBuilder.RecurringDaily().Build();
        var prefix = "Occurs every day. ";
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse(prefix, nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        // "Occurs every day. at 10:00. Starting on 12/05/2026"
        Assert.Contains("at 10:00", result);
        Assert.Contains("Starting on 12/05/2026", result);
        Assert.StartsWith(prefix, result);
    }

    [Fact]
    public void Format_Converts_To_Local_Time_In_Description()
    {
        // Arrange
        // May 12, 2026 -> On this date CST uses Daylight Saving Time (UTC-5)
        // 10:00 AM UTC - 5h = 05:00 AM
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().Build();
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse("Prefix. ", nextExecution, config, _cstZone, culture);

        // Assert
        // We change to 05:00 because it is the actual local time in May for that zone
        Assert.Contains("at 05:00", result);
    }

    [Fact]
    public void Includes_Daily_Frequency_Details_When_Present()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse("Daily. ", nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        Assert.Contains("Every 2 hours", result);
    }

    [Fact]
    public void Description_Should_Be_Coherent_With_Spanish_Culture()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().Build();
        var culture = new CultureInfo("es-ES");
        var prefix = "Ocurre cada día. ";

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse(prefix, nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        // Note: If you haven't implemented the translation of "at" and "Starting on" in the rule,
        // this test will fail or confirm that they are still in English.
        Assert.StartsWith("Ocurre cada día.", result);
        Assert.Contains("12/05/2026", result);
    }
}