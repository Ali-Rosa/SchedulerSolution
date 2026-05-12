using Scheduler.Domain.Rules;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Shouldly;

namespace Scheduler.Domain.Tests.Rules;

public class CultureRuleTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void Standard_Locales_Should_Be_Valid(string locale)
    {
        // Act
        var result = CultureRule.IsValid(locale);

        // Assert
        result.ShouldBeTrue($"The {locale} culture should be recognized by the system.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invented-culture")]
    [InlineData("12345")]
    public void Invalid_Locales_Should_Not_Be_Valid(string? locale)
    {
        // Act
        var result = CultureRule.IsValid(locale!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void FirstDayOfWeek_Should_Match_Culture_Defaults_When_Not_Overridden()
    {
        // Arrange
        var configUS = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").Build();
        var configES = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("es-ES").Build();

        // Act
        var firstDayUS = CultureRule.GetFirstDayOfWeek(configUS);
        var firstDayES = CultureRule.GetFirstDayOfWeek(configES);

        // Assert
        firstDayUS.ShouldBe(DayOfWeek.Sunday); // US Standard
        firstDayES.ShouldBe(DayOfWeek.Monday); // Spanish/ISO Standard
    }

    [Fact]
    public void FirstDayOfWeek_Should_Use_Overridden_Value_Regardless_Of_Culture()
    {
        // Arrange: Culture is US (Sunday), but we manually forced Monday
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_FirstDayOfWeek(DayOfWeek.Monday)
            .Build();

        // Act
        var result = CultureRule.GetFirstDayOfWeek(config);

        // Assert
        result.ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public void GetCultureInfo_Should_Return_Correct_Instance_For_Valid_Locale()
    {
        // Arrange
        var locale = "es-MX";

        // Act
        var culture = CultureRule.GetCultureInfo(locale);

        // Assert
        culture.ShouldNotBeNull();
        culture.Name.ShouldBe(locale);
    }

}