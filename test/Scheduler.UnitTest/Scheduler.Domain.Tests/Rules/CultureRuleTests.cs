using System.Globalization;
using Scheduler.Domain.Rules;
using Scheduler.Domain.Tests.TestHelpers.Builders;

namespace Scheduler.Domain.Tests.Rules;

public class CultureRuleTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void IsValid_Returns_True_For_Standard_Cultures(string locale)
    {
        // Act
        var result = CultureRule.IsValid(locale);

        // Assert
        Assert.True(result, $"The {locale} culture should be valid in any system.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invented-culture")]
    [InlineData("12345")]
    public void IsValid_Returns_False_For_Invalid_Cultures(string locale)
    {
        // Act
        var result = CultureRule.IsValid(locale);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetFirstDayOfWeek_Returns_Culture_Default_When_Not_Forced()
    {
        // Arrange
        var configUS = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").Build();
        var configES = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("es-ES").Build();

        // Act
        var firstDayUS = CultureRule.GetFirstDayOfWeek(configUS);
        var firstDayES = CultureRule.GetFirstDayOfWeek(configES);

        // Assert
        Assert.Equal(DayOfWeek.Sunday, firstDayUS); // In US, the week starts on Sunday
        Assert.Equal(DayOfWeek.Monday, firstDayES); // In ES, the week starts on Monday
    }

    [Fact]
    public void GetFirstDayOfWeek_Returns_Forced_Value_Ignoring_Culture()
    {
        // Arrange: Culture is US (Sunday), but we force Monday
        var config = ScheduleConfigurationBuilder.RecurringDaily()
            .With_Locale("en-US")
            .With_FirstDayOfWeek(DayOfWeek.Monday)
            .Build();

        // Act
        var result = CultureRule.GetFirstDayOfWeek(config);

        // Assert
        Assert.Equal(DayOfWeek.Monday, result);
    }

    [Fact]
    public void GetCultureInfo_Returns_Valid_Instance()
    {
        // Act
        var culture = CultureRule.GetCultureInfo("es-MX");

        // Assert
        Assert.NotNull(culture);
        Assert.Equal("es-MX", culture.Name);
    }
}