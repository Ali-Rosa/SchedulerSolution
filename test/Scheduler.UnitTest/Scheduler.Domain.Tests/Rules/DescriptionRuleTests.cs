using System.Globalization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class DescriptionRuleTests
{
    private readonly TimeZoneInfo _cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"); // UTC-6 / UTC-5 in Summer

    [Fact]
    public void Basic_Description_Should_Include_Time_And_Start_Date()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero); // 10:00 UTC
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").Build();
        var prefix = "Occurs every day. ";
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse(prefix, nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        result.ShouldStartWith(prefix);
        result.ShouldContain("at 10:00");
        result.ShouldContain("Starting on 12/05/2026");
    }

    [Fact]
    public void Description_Should_Display_Local_Time_Based_On_TimeZone()
    {
        // Arrange
        // May 12, 2026 -> On this date CST uses Daylight Saving Time (UTC-5)
        // 10:00 AM UTC - 5h = 05:00 AM
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").Build();
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse("Prefix. ", nextExecution, config, _cstZone, culture);

        // Assert
        result.ShouldContain("at 05:00");
    }

    [Fact]
    public void Description_Should_Include_IntraDay_Frequency_Details()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(4, 0), new TimeOnly(8, 0))
            .Build();
        var culture = new CultureInfo("en-US");

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse("Daily. ", nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        result.ShouldContain("Every 2 hours");
    }

    [Fact]
    public void Description_Should_Apply_Cultural_Date_Formatting()
    {
        // Arrange
        var nextExecution = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("es-ES").Build();
        var culture = new CultureInfo("es-ES");
        var prefix = "Ocurre cada día. ";

        // Act
        var result = DescriptionRule.FormatMenssajeDescrictionResponse(prefix, nextExecution, config, TimeZoneInfo.Utc, culture);

        // Assert
        result.ShouldStartWith("Ocurre cada día.");
        result.ShouldContain("12/05/2026");
        // Note: If you implemented translations for "at" and "Starting on"
        // Here you could validate with .ShouldContain("a las") and .ShouldContain("Empezando el")
    }

}