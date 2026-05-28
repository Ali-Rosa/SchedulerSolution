using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_Tests
{
    private readonly SchedulerService _service = new([new RecurringWeeklySchedulerStrategy()]);

    #region Validation Tests
    #endregion Validation Tests

    #region Core Logic & Anchor Time





    #endregion Core Logic & Anchor Time

    #region Week Skipping (RecursEvery)

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IPattern_Should_Skip_Weeks_According_To_Recursion_Value()
    {
        // Monday 04 at 00:01. Every 2 weeks, Monday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        // Monday 04 (1 min passed) -> Monday 11 (Week 1, Skip) -> Monday 18 (Week 2)
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(18);
    }

    [Theory]
    [InlineData(0, 12, 6)]  // Lunes 04 a las 12:00 PM -> Debe caer el miércoles 06 (Misma semana 0)
    [InlineData(4, 12, 25)] // Viernes 08 a las 12:00 PM -> Salta semana 1 y 2 -> Debe caer el lunes 25 (Semana 3)
    public void CalculateNextExecution_RecurringWeekly_WithMultipleAllowedDays_ShouldFindCorrectNextExecution(int offsetDays, int offsetHours, int expectedDay)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero).AddDays(offsetDays).AddHours(offsetHours),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 3,
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
    }

    #endregion Week Skipping (RecursEvery)

    #region FirstDayOfWeek Impact

    [Theory]
    [InlineData(DayOfWeek.Monday, 18)] // Empieza en Lunes -> El Lunes 11 es Semana 1 (Se salta) -> Cae el Lunes 18 (Semana 2)
    [InlineData(DayOfWeek.Thursday, 11)] // Empieza en Jueves -> El Lunes 11 es Semana 0 (Coincidencia/Hit) -> Cae el Lunes 11
    public void CalculateNextExecution_RecurringWeekly_FirstDayOfWeekBoundary_ShouldDetermineCorrectWeekGroup(DayOfWeek firstDayOfWeek, int expectedDay)
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Jueves 07 de mayo
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
            FirstDayOfWeek = firstDayOfWeek // Parametrizado mediante la teoría
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
    }

    #endregion FirstDayOfWeek Impact

    #region Calendar Edge Cases

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Handle_Year_Transition_Correctly()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero).AddMinutes(1),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = new DateTimeOffset(2025, 12, 22, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Day.ShouldBe(5);
    }

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IShould_Handle_Leap_Year_February_Correctly()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero).AddMinutes(1),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Thursday] },
            LimitsStartDateLocal = new DateTimeOffset(2024, 2, 22, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Month.ShouldBe(3);
        result.NextExecutionTime.Value.Day.ShouldBe(7);
    }

    #endregion Calendar Edge Cases

    #region Localization & Description Tests

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IDescription_Should_Use_Cultural_Format()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "ru-RU", // Russian culture
        };

        var result = _service.CalculateNextExecution(config);

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("2026");
    }

    #endregion Localization & Description Tests

    #region Limits & Safety

    [Fact]
    public void Calculate_NextExecution_Recurring_Weekly_Without_DailyFrequency_IMotor_Should_Stop_At_EndDate_Limit()
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] }, // Next Friday is 2026-05-08
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Before the next execution
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US"
        };

        var result = _service.CalculateNextExecution(config);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
    }

    #endregion Limits & Safety

}