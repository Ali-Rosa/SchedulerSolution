using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Recurring_Monthly_Without_DailyFrequency_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_MissingConfiguration_ReturnsErrorMessage()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly().With_Locale("en-US").Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Monthly configuration is required");
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_SpecificDay_InheritsAnchorTime()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 2, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(14);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_PassedDay_JumpsToNextMonth()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlySpecificDay(10)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(6);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_RelativeDay_ReturnsExpectedDescription()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(3)
            .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.Last, SchedulerMonthlyRelativeDayType.WeekendDay)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("Occurs the last weekend day of every 3 months");
    }

    [Theory]
    [InlineData(SchedulerMonthlyRelativeOrdinal.Second, SchedulerMonthlyRelativeDayType.Monday, "8,10")]
    public void CalculateNextExecution_RecurringMonthly_WithDailyFrequencySequence_ReturnsExpectedHours(
        SchedulerMonthlyRelativeOrdinal ordinal,
        SchedulerMonthlyRelativeDayType dayType,
        string expectedHoursCsv)
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHours = expectedHoursCsv.Split(',').Select(int.Parse).ToList();

        var config = ScheduleConfigurationBuilder.RecurringMonthly()
            .With_Locale("en-US")
            .With_RecursEvery(1)
            .With_MonthlyRelativeDay(ordinal, dayType)
            .With_DailyFrequency_OccursEvery(SchedulerTimeIntervalUnit.Hours, 2, new TimeOnly(8, 0), new TimeOnly(10, 0))
            .With_Limits_StartDateLocal(startDate)
            .Build();

        // Act & Assert
        DateTimeOffset lastExecution = startDate;

        foreach (var expectedHour in expectedHours)
        {
            var result = _service.CalculateNextExecution(lastExecution, config);

            result.IsSuccess.ShouldBeTrue();
            result.NextExecutionTime.ShouldNotBeNull();
            result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
            result.NextExecutionTime.Value.Minute.ShouldBe(0);

            lastExecution = result.NextExecutionTime.Value;
        }
    }

    [Fact]
    public void CalculateNextExecution_RecurringMonthly_InvalidCulture_ReturnsError()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.RecurringMonthly().With_Locale("invalid-culture").Build();
        
        
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);
        
        
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not supported");
    }

    //[Fact]
    //public void CalculateNextExecution_RecurringMonthly_ZeroRecursEvery_ReturnsError()
    //{
    //    // Arrange
    //    var config = ScheduleConfigurationBuilder.RecurringMonthly().With_Locale("en-US").With_RecursEvery(0).Build();
        
        
    //    var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);
        
        
    //    result.IsSuccess.ShouldBeFalse();
    //    result.ErrorMessage.ShouldContain("greater than 0");
    //}

    //[Fact]
    //public void BuildMonthlyDescription_ShouldHandleBothBranches()
    //{
    //    // 1. Prueba "Every month" (recursEvery == 1) y "Specific Day"
    //    var configSpecific = ScheduleConfigurationBuilder.RecurringMonthly()
    //        .With_RecursEvery(1)
    //        .With_MonthlySpecificDay(10)
    //        .Build();

    //    // 2. Prueba "Every X months" (recursEvery > 1) y "Relative Day"
    //    var configRelative = ScheduleConfigurationBuilder.RecurringMonthly()
    //        .With_RecursEvery(2)
    //        .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, SchedulerMonthlyRelativeDayType.Monday)
    //        .Build();

    //    var resultSpecific = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configSpecific);
    //    var resultRelative = _service.CalculateNextExecution(DateTimeOffset.UtcNow, configRelative);

    //    resultSpecific.Description.ShouldContain("every month");
    //    resultRelative.Description.ShouldContain("every 2 months");
    //}

    //[Fact]
    //public void FormatDayType_InvalidEnum_ReturnsDefaultString()
    //{
    //    // Forzamos un valor que no existe en el Enum (asumiendo que SchedulerMonthlyRelativeDayType tiene valores 0-12)
    //    var config = ScheduleConfigurationBuilder.RecurringMonthly()
    //        .With_MonthlyRelativeDay(SchedulerMonthlyRelativeOrdinal.First, (SchedulerMonthlyRelativeDayType)99)
    //        .Build();

    //    var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

    //    // Esto entrará en el 'default' del switch, cubriendo esa línea roja
    //    result.IsSuccess.ShouldBeTrue();
    //    result.Description.ShouldContain("99"); // El ToString() de 99 devolverá "99"
    //}

}