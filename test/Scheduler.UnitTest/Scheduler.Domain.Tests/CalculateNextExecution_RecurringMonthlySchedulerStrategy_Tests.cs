using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Models.Monthly;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_RecurringMonthlySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new RecurringMonthlySchedulerStrategy()]);

    #region Validation

    [Theory]
    [InlineData("en-US", "Monthly configuration is required for Monthly recurring schedules.")]
    [InlineData("en-GB", "Monthly configuration is required for Monthly recurring schedules.")]
    [InlineData("es-ES", "La configuración mensual es requerida para planificaciones recurrentes mensuales.")]
    public void CalculateNextExecution_WhenMonthlyConfigurationIsMissing_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();

        // Verify that the validation error message is translated correctly according to the culture
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    // first ordinal
    [InlineData(MonthlyRelativeOrdinal.First, "en-US", "first weekday")]
    [InlineData(MonthlyRelativeOrdinal.First, "en-GB", "first weekday")]
    [InlineData(MonthlyRelativeOrdinal.First, "es-ES", "primer día de la semana")]
    // second ordinal
    [InlineData(MonthlyRelativeOrdinal.Second, "en-US", "second weekday")]
    [InlineData(MonthlyRelativeOrdinal.Second, "en-GB", "second weekday")]
    [InlineData(MonthlyRelativeOrdinal.Second, "es-ES", "segundo día de la semana")]
    // third ordinal
    [InlineData(MonthlyRelativeOrdinal.Third, "en-US", "third weekday")]
    [InlineData(MonthlyRelativeOrdinal.Third, "en-GB", "third weekday")]
    [InlineData(MonthlyRelativeOrdinal.Third, "es-ES", "tercer día de la semana")]
    // fourth ordinal
    [InlineData(MonthlyRelativeOrdinal.Fourth, "en-US", "fourth weekday")]
    [InlineData(MonthlyRelativeOrdinal.Fourth, "en-GB", "fourth weekday")]
    [InlineData(MonthlyRelativeOrdinal.Fourth, "es-ES", "cuarto día de la semana")]
    // last ordinal
    [InlineData(MonthlyRelativeOrdinal.Last, "en-US", "last weekday")]
    [InlineData(MonthlyRelativeOrdinal.Last, "en-GB", "last weekday")]
    [InlineData(MonthlyRelativeOrdinal.Last, "es-ES", "último día de la semana")]
    public void CalculateNextExecution_WhenAllOrdinalsProvided_ReturnsValidExecution(
            MonthlyRelativeOrdinal ordinal,
            string locale,
            string expectedOrdinalPhrase )
    {
        // Arrange - Coverage for all possible ordinals in a localized manner
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = ordinal,
                RelativeDayType = MonthlyRelativeDayType.Weekday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.Description.ShouldNotBeNullOrWhiteSpace();

        // Verify that the relative ordinal prefix is translated and formatted according to the culture
        result.Description.ShouldContain(expectedOrdinalPhrase);
    }


    [Theory]
    [InlineData("en-US", "Occurs the second friday of every month", "10:00 AM", "06-12-2026")]
    [InlineData("en-GB", "Occurs the second friday of every month", "10:00", "12/06/2026")]
    [InlineData("es-ES", "Ocurre el segundo viernes de cada mes", "10:00", "12/06/2026")]
    public void CalculateNextExecution_WhenSpecificDayOfWeekConfigured_ReturnsCorrectDay(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange - Deterministic coverage for the conversion of specific days (second Friday of June 2026)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.Friday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that it falls exactly on the second Friday of the month (Friday, June 12, 2026) at 10:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(6);
        result.NextExecutionTime.Value.Day.ShouldBe(12);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.NextExecutionTime.Value.DayOfWeek.ShouldBe(DayOfWeek.Friday);

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Not defined relative day type: 999.")]
    [InlineData("en-GB", "Not defined relative day type: 999.")]
    [InlineData("es-ES", "Tipo de día relativo no definido: 999.")]
    public void CalculateNextExecution_WhenRelativeDayTypeIsInvalid_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = (MonthlyRelativeDayType)999 // Invalid value
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();

        // Verify that the validation error message contains the value and the correct translation according to the locale
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    [InlineData("en-US", "Not defined relative ordinal: 999.")]
    [InlineData("en-GB", "Not defined relative ordinal: 999.")]
    [InlineData("es-ES", "Ordinal relativo no definido: 999.")]
    public void CalculateNextExecution_WhenRelativeOrdinalIsInvalid_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = (MonthlyRelativeOrdinal)999, // Invalid value
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();

        // Verify that the validation error message contains the value and the correct translation according to the locale
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    // Cases with 50 (Exceeds the maximum limit)
    [InlineData(50, "en-US", "The day must be between 1 and 31.")]
    [InlineData(50, "en-GB", "The day must be between 1 and 31.")]
    [InlineData(50, "es-ES", "El día debe estar entre 1 y 31.")]
    // Cases with 0 (Below the minimum limit)
    [InlineData(0, "en-US", "The day must be between 1 and 31.")]
    [InlineData(0, "en-GB", "The day must be between 1 and 31.")]
    [InlineData(0, "es-ES", "El día debe estar entre 1 y 31.")]
    // Cases with Null (Null / missing value)
    [InlineData(null, "en-US", "The day must be between 1 and 31.")]
    [InlineData(null, "en-GB", "The day must be between 1 and 31.")]
    [InlineData(null, "es-ES", "El día debe estar entre 1 y 31.")]
    public void CalculateNextExecution_WhenSpecificDayIsInvalid_ReturnsError(
            int? specificDayNumber,
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = specificDayNumber
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();

        // Verify that the validation error message is returned in the corresponding language
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    #endregion Validation

    #region OccursOnce Mode


    [Theory]
    [InlineData("en-US", "Occurs day 15 of every month", "2:30 PM", "05-15-2026")]
    [InlineData("en-GB", "Occurs day 15 of every month", "14:30", "15/05/2026")]
    [InlineData("es-ES", "Ocurre el día 15 de cada mes", "14:30", "15/05/2026")]
    public void CalculateNextExecution_WhenOccursOnceWithSpecificDay_ReturnsCorrectExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: May 15 at 2:30 PM UTC
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(15);
        result.NextExecutionTime.Value.Hour.ShouldBe(14);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/01/2026 00:00) -> Falls on the same 01 of January at 09:00 AM
    [InlineData(2026, 1, 1, 0, 2026, 1, 1, 9, "en-US", "Occurs day 1 of every 3 months", "9:00 AM", "01-01-2026")]
    [InlineData(2026, 1, 1, 0, 2026, 1, 1, 9, "en-GB", "Occurs day 1 of every 3 months", "09:00", "01/01/2026")]
    [InlineData(2026, 1, 1, 0, 2026, 1, 1, 9, "es-ES", "Ocurre el día 1 de cada 3 meses", "09:00", "01/01/2026")]
    // Step 2: Query on January 01 at 09:00 AM -> Jumps 3 months -> Falls on April 01 at 09:00 AM
    [InlineData(2026, 1, 1, 9, 2026, 4, 1, 9, "en-US", "Occurs day 1 of every 3 months", "9:00 AM", "04-01-2026")]
    [InlineData(2026, 1, 1, 9, 2026, 4, 1, 9, "en-GB", "Occurs day 1 of every 3 months", "09:00", "01/04/2026")]
    [InlineData(2026, 1, 1, 9, 2026, 4, 1, 9, "es-ES", "Ocurre el día 1 de cada 3 meses", "09:00", "01/04/2026")]
    // Step 3: Query on April 01 at 09:00 AM -> Jumps 3 months -> Falls on July 01 at 09:00 AM
    [InlineData(2026, 4, 1, 9, 2026, 7, 1, 9, "en-US", "Occurs day 1 of every 3 months", "9:00 AM", "07-01-2026")]
    [InlineData(2026, 4, 1, 9, 2026, 7, 1, 9, "en-GB", "Occurs day 1 of every 3 months", "09:00", "01/07/2026")]
    [InlineData(2026, 4, 1, 9, 2026, 7, 1, 9, "es-ES", "Ocurre el día 1 de cada 3 meses", "09:00", "01/07/2026")]
    public void CalculateNextExecution_WhenOccursOnceAndRecurringMonths_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 1
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs the last weekday of every month", "5:00 PM", "05-29-2026")]
    [InlineData("en-GB", "Occurs the last weekday of every month", "17:00", "29/05/2026")]
    [InlineData("es-ES", "Ocurre el último día de la semana de cada mes", "17:00", "29/05/2026")]
    public void CalculateNextExecution_WhenOccursOnceWithRelativeWeekday_ReturnsCorrectExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Query on May 1 at 10:00 AM. The last weekday of May 2026 is Friday, May 29.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.Weekday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(17, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The last weekday of May at 17:00 UTC (Friday, May 29)
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(29);
        result.NextExecutionTime.Value.Hour.ShouldBe(17);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);

        // The day of the week must be a weekday (Monday-Friday), not Saturday or Sunday
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Saturday);
        result.NextExecutionTime.Value.DayOfWeek.ShouldNotBe(DayOfWeek.Sunday);

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion OccursOnce Mode

    #region OccursEvery Mode


    [Theory]
    // Step 1: Initial query (01/01/2020 00:00) -> Falls on the 1st Thursday at the 1st hour (02/01/2020 03:00)
    [InlineData(2020, 1, 1, 0, 2020, 1, 2, 3, "en-US", "Occurs the first thursday of every 3 months", "Every 1 hour", "3:00 AM", "01-02-2020")]
    [InlineData(2020, 1, 1, 0, 2020, 1, 2, 3, "en-GB", "Occurs the first thursday of every 3 months", "Every 1 hour", "03:00", "02/01/2020")]
    [InlineData(2020, 1, 1, 0, 2020, 1, 2, 3, "es-ES", "Ocurre el primer jueves de cada 3 meses", "Cada 1 hora", "03:00", "02/01/2020")]
    // Step 2: Already executed at 3:00, query at that moment -> Falls on the same day at 04:00 (2nd hour)
    [InlineData(2020, 1, 2, 3, 2020, 1, 2, 4, "en-US", "Occurs the first thursday of every 3 months", "Every 1 hour", "4:00 AM", "01-02-2020")]
    [InlineData(2020, 1, 2, 3, 2020, 1, 2, 4, "en-GB", "Occurs the first thursday of every 3 months", "Every 1 hour", "04:00", "02/01/2020")]
    [InlineData(2020, 1, 2, 3, 2020, 1, 2, 4, "es-ES", "Ocurre el primer jueves de cada 3 meses", "Cada 1 hora", "04:00", "02/01/2020")]
    // Step 3: Query at 04:00 -> Falls on the same day at 05:00 (3rd hour)
    [InlineData(2020, 1, 2, 4, 2020, 1, 2, 5, "en-US", "Occurs the first thursday of every 3 months", "Every 1 hour", "5:00 AM", "01-02-2020")]
    [InlineData(2020, 1, 2, 4, 2020, 1, 2, 5, "en-GB", "Occurs the first thursday of every 3 months", "Every 1 hour", "05:00", "02/01/2020")]
    [InlineData(2020, 1, 2, 4, 2020, 1, 2, 5, "es-ES", "Ocurre el primer jueves de cada 3 meses", "Cada 1 hora", "05:00", "02/01/2020")]
    // Step 4: Query at 05:00 -> Falls on the same day at 06:00 (last hour of the day)
    [InlineData(2020, 1, 2, 5, 2020, 1, 2, 6, "en-US", "Occurs the first thursday of every 3 months", "Every 1 hour", "6:00 AM", "01-02-2020")]
    [InlineData(2020, 1, 2, 5, 2020, 1, 2, 6, "en-GB", "Occurs the first thursday of every 3 months", "Every 1 hour", "06:00", "02/01/2020")]
    [InlineData(2020, 1, 2, 5, 2020, 1, 2, 6, "es-ES", "Ocurre el primer jueves de cada 3 meses", "Cada 1 hora", "06:00", "02/01/2020")]
    // Step 5: The day is exhausted (query at 06:00) -> Jumps 3 months to APRIL (02/04/2020 03:00)
    [InlineData(2020, 1, 2, 6, 2020, 4, 2, 3, "en-US", "Occurs the first thursday of every 3 months", "Every 1 hour", "3:00 AM", "04-02-2020")]
    [InlineData(2020, 1, 2, 6, 2020, 4, 2, 3, "en-GB", "Occurs the first thursday of every 3 months", "Every 1 hour", "03:00", "02/04/2020")]
    [InlineData(2020, 1, 2, 6, 2020, 4, 2, 3, "es-ES", "Ocurre el primer jueves de cada 3 meses", "Cada 1 hora", "03:00", "02/04/2020")]
    public void CalculateNextExecution_WhenOccursEveryWithHours_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Thursday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(3, 0),
                EndTime = new TimeOnly(6, 0)
            },
            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/01/2020 00:00) -> Falls on Sunday 05/01 (2nd weekend) at 03:00
    [InlineData(2020, 1, 1, 0, 2020, 1, 5, 3, "en-US", "Occurs the second weekend day of every month", "Every 1 hour", "3:00 AM", "01-05-2020")]
    [InlineData(2020, 1, 1, 0, 2020, 1, 5, 3, "en-GB", "Occurs the second weekend day of every month", "Every 1 hour", "03:00", "05/01/2020")]
    [InlineData(2020, 1, 1, 0, 2020, 1, 5, 3, "es-ES", "Ocurre el segundo día de fin de semana de cada mes", "Cada 1 hora", "03:00", "05/01/2020")]
    // Step 2: Query the same Sunday 05/01 at 05:00 -> Falls on the same day at the last hour (06:00)
    [InlineData(2020, 1, 5, 5, 2020, 1, 5, 6, "en-US", "Occurs the second weekend day of every month", "Every 1 hour", "6:00 AM", "01-05-2020")]
    [InlineData(2020, 1, 5, 5, 2020, 1, 5, 6, "en-GB", "Occurs the second weekend day of every month", "Every 1 hour", "06:00", "05/01/2020")]
    [InlineData(2020, 1, 5, 5, 2020, 1, 5, 6, "es-ES", "Ocurre el segundo día de fin de semana de cada mes", "Cada 1 hora", "06:00", "05/01/2020")]
    // Step 3: January day exhausted (query at 06:00) -> Jumps to February (Sunday 02/02 at 03:00)
    [InlineData(2020, 1, 5, 6, 2020, 2, 2, 3, "en-US", "Occurs the second weekend day of every month", "Every 1 hour", "3:00 AM", "02-02-2020")]
    [InlineData(2020, 1, 5, 6, 2020, 2, 2, 3, "en-GB", "Occurs the second weekend day of every month", "Every 1 hour", "03:00", "02/02/2020")]
    [InlineData(2020, 1, 5, 6, 2020, 2, 2, 3, "es-ES", "Ocurre el segundo día de fin de semana de cada mes", "Cada 1 hora", "03:00", "02/02/2020")]
    // Step 4: February day exhausted (query at 06:00) -> Jumps to March (Saturday 07/03 at 03:00)
    [InlineData(2020, 2, 2, 6, 2020, 3, 7, 3, "en-US", "Occurs the second weekend day of every month", "Every 1 hour", "3:00 AM", "03-07-2020")]
    [InlineData(2020, 2, 2, 6, 2020, 3, 7, 3, "en-GB", "Occurs the second weekend day of every month", "Every 1 hour", "03:00", "07/03/2020")]
    [InlineData(2020, 2, 2, 6, 2020, 3, 7, 3, "es-ES", "Ocurre el segundo día de fin de semana de cada mes", "Cada 1 hora", "03:00", "07/03/2020")]
    public void CalculateNextExecution_WhenRelativeWeekendDayWithIntervals_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.WeekendDay
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(3, 0),
                EndTime = new TimeOnly(6, 0)
            },
            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/01/2026 00:00) -> Falls on January 15th at 08:00 AM
    [InlineData(2026, 1, 1, 0, 2026, 1, 15, 8, "en-US", "Occurs day 15 of every 2 months", "Every 4 hours", "8:00 AM", "01-15-2026")]
    [InlineData(2026, 1, 1, 0, 2026, 1, 15, 8, "en-GB", "Occurs day 15 of every 2 months", "Every 4 hours", "08:00", "15/01/2026")]
    [InlineData(2026, 1, 1, 0, 2026, 1, 15, 8, "es-ES", "Ocurre el día 15 de cada 2 meses", "Cada 4 horas", "08:00", "15/01/2026")]
    // Step 2: Query on 15/01 at 08:00 -> Falls on the same day at 12:00 (noon)
    [InlineData(2026, 1, 15, 8, 2026, 1, 15, 12, "en-US", "Occurs day 15 of every 2 months", "Every 4 hours", "12:00 PM", "01-15-2026")]
    [InlineData(2026, 1, 15, 8, 2026, 1, 15, 12, "en-GB", "Occurs day 15 of every 2 months", "Every 4 hours", "12:00", "15/01/2026")]
    [InlineData(2026, 1, 15, 8, 2026, 1, 15, 12, "es-ES", "Ocurre el día 15 de cada 2 meses", "Cada 4 horas", "12:00", "15/01/2026")]
    // Step 3: Query on 15/01 at 12:00 -> Falls on the same day at 16:00 (afternoon)
    [InlineData(2026, 1, 15, 12, 2026, 1, 15, 16, "en-US", "Occurs day 15 of every 2 months", "Every 4 hours", "4:00 PM", "01-15-2026")]
    [InlineData(2026, 1, 15, 12, 2026, 1, 15, 16, "en-GB", "Occurs day 15 of every 2 months", "Every 4 hours", "16:00", "15/01/2026")]
    [InlineData(2026, 1, 15, 12, 2026, 1, 15, 16, "es-ES", "Ocurre el día 15 de cada 2 meses", "Cada 4 horas", "16:00", "15/01/2026")]
    // Step 4: Query on 15/01 at 16:00 -> Falls on the same day at 20:00 (last of the day)
    [InlineData(2026, 1, 15, 16, 2026, 1, 15, 20, "en-US", "Occurs day 15 of every 2 months", "Every 4 hours", "8:00 PM", "01-15-2026")]
    [InlineData(2026, 1, 15, 16, 2026, 1, 15, 20, "en-GB", "Occurs day 15 of every 2 months", "Every 4 hours", "20:00", "15/01/2026")]
    [InlineData(2026, 1, 15, 16, 2026, 1, 15, 20, "es-ES", "Ocurre el día 15 de cada 2 meses", "Cada 4 horas", "20:00", "15/01/2026")]
    // Step 5: The day of January is exhausted (query at 20:00) -> Jumps 2 months -> Falls on March 15th at 08:00 AM
    [InlineData(2026, 1, 15, 20, 2026, 3, 15, 8, "en-US", "Occurs day 15 of every 2 months", "Every 4 hours", "8:00 AM", "03-15-2026")]
    [InlineData(2026, 1, 15, 20, 2026, 3, 15, 8, "en-GB", "Occurs day 15 of every 2 months", "Every 4 hours", "08:00", "15/03/2026")]
    [InlineData(2026, 1, 15, 20, 2026, 3, 15, 8, "es-ES", "Ocurre el día 15 de cada 2 meses", "Cada 4 horas", "08:00", "15/03/2026")]
    public void CalculateNextExecution_WhenOccursEveryWithTwoMonthInterval_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 2, // Every 2 months
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 4,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(20, 0)
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion OccursEvery Mode

    #region Monthly Pattern (Specific Day)

    
    [Theory]
    [InlineData("en-US", "Occurs day 10 of every month", "2:30 PM", "05-10-2026")]
    [InlineData("en-GB", "Occurs day 10 of every month", "14:30", "10/05/2026")]
    [InlineData("es-ES", "Ocurre el día 10 de cada mes", "14:30", "10/05/2026")]
    public void CalculateNextExecution_WhenSpecificDay_UsesAnchorTime(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Check on May 2nd at 2:30 PM. Next execution: May 10th.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 2, 14, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(14);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);

        // Verify the localized output description
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 10 of every month", "10:00 AM", "06-10-2026")]
    [InlineData("en-GB", "Occurs day 10 of every month", "10:00", "10/06/2026")]
    [InlineData("es-ES", "Ocurre el día 10 de cada mes", "10:00", "10/06/2026")]
    public void CalculateNextExecution_WhenSpecificDayHasPassed_ReturnsNextMonth(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Check on May 20th. The 10th has already passed. Should jump to June.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(6);
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);

        // Verify the localized output description
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 31 of every month", "10:00 AM", "03-31-2026")]
    [InlineData("en-GB", "Occurs day 31 of every month", "10:00", "31/03/2026")]
    [InlineData("es-ES", "Ocurre el día 31 de cada mes", "10:00", "31/03/2026")]
    public void CalculateNextExecution_WhenSpecificDayDoesNotExistWithoutDailyFrequency_SkipsMonth(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Check on February 1st. The 31st does not exist in February, should jump to March.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: February does not have a 31st, so the engine should jump to March.
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(3); // March
        result.NextExecutionTime.Value.Day.ShouldBe(31);  // Day 31
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);

        // Verify the localized output description
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Monthly Pattern (Specific Day)

    #region Monthly Pattern (Relative Day)


    [Theory]
    // Case 1: Every month, first weekday of the month
    [InlineData(1, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Weekday, "en-US", "the first weekday of every month")]
    [InlineData(1, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Weekday, "en-GB", "the first weekday of every month")]
    [InlineData(1, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Weekday, "es-ES", "el primer día de la semana de cada mes")]
    // Case 2: Every 2 months, last day of the month
    [InlineData(2, MonthlyRelativeOrdinal.Last, MonthlyRelativeDayType.Day, "en-US", "the last day of every 2 months")]
    [InlineData(2, MonthlyRelativeOrdinal.Last, MonthlyRelativeDayType.Day, "en-GB", "the last day of every 2 months")]
    [InlineData(2, MonthlyRelativeOrdinal.Last, MonthlyRelativeDayType.Day, "es-ES", "el último día de cada 2 meses")]
    // Case 3: Every month, third day of the month (day)
    [InlineData(1, MonthlyRelativeOrdinal.Third, MonthlyRelativeDayType.Day, "en-US", "the third day of every month")]
    [InlineData(1, MonthlyRelativeOrdinal.Third, MonthlyRelativeDayType.Day, "en-GB", "the third day of every month")]
    [InlineData(1, MonthlyRelativeOrdinal.Third, MonthlyRelativeDayType.Day, "es-ES", "el tercer día de cada mes")]
    public void CalculateNextExecution_WhenRelativeDayConfiguration_GeneratesCorrectDescription(
            int recursEvery,
            MonthlyRelativeOrdinal relativeOrdinal,
            MonthlyRelativeDayType relativeDayType,
            string locale,
            string expectedDescription )
    {
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = recursEvery,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = relativeOrdinal,
                RelativeDayType = relativeDayType
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that the description contains the translated pattern structure
        result.Description.ShouldContain(expectedDescription);
    }


    #endregion Monthly Pattern (Relative Day)

    #region Month Recurrence (RecursEvery)


    [Theory]
    [InlineData("en-US", "Occurs day 10 of every 3 months", "10:00 AM", "05-10-2026")]
    [InlineData("en-GB", "Occurs day 10 of every 3 months", "10:00", "10/05/2026")]
    [InlineData("es-ES", "Ocurre el día 10 de cada 3 meses", "10:00", "10/05/2026")]
    public void CalculateNextExecution_WhenRecurringEveryMonths_ReturnsFirstExecutionInStartMonth(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange - Every 3 months, starting on May 1, looking for May 10 (should execute in the start month)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that it remains in the start month (month 5) at 10:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5); // First execution in May
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs the first monday of every month", "10:00 AM", "04-06-2026")]
    [InlineData("en-GB", "Occurs the first monday of every month", "10:00", "06/04/2026")]
    [InlineData("es-ES", "Ocurre el primer lunes de cada mes", "10:00", "06/04/2026")]
    public void CalculateNextExecution_WhenNoValidRelativeDayInMonth_SkipsToNextMonth(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange - Query on April 1 at 10:00 AM. The first Monday of April 2026 is the 6th.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the accuracy of the calculation for the first Monday of April (Monday, April 6, 2026) at 10:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(4);
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.NextExecutionTime.Value.DayOfWeek.ShouldBe(DayOfWeek.Monday);

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Month Recurrence (RecursEvery)

    #region Edge Cases & Boundaries


    [Theory]
    // Step 1: Initial query (15/02/2026 10:00) -> Jumps to March (31/03/2026 at 17:00)
    [InlineData(2026, 2, 15, 10, 2026, 3, 31, 17, "en-US", "Occurs day 31 of every month", "Every 1 hour", "5:00 PM", "03-31-2026")]
    [InlineData(2026, 2, 15, 10, 2026, 3, 31, 17, "en-GB", "Occurs day 31 of every month", "Every 1 hour", "17:00", "31/03/2026")]
    [InlineData(2026, 2, 15, 10, 2026, 3, 31, 17, "es-ES", "Ocurre el día 31 de cada mes", "Cada 1 hora", "17:00", "31/03/2026")]
    // Step 2: Query on 31/03 at 17:00 -> Falls on the same day at 18:00
    [InlineData(2026, 3, 31, 17, 2026, 3, 31, 18, "en-US", "Occurs day 31 of every month", "Every 1 hour", "6:00 PM", "03-31-2026")]
    [InlineData(2026, 3, 31, 17, 2026, 3, 31, 18, "en-GB", "Occurs day 31 of every month", "Every 1 hour", "18:00", "31/03/2026")]
    [InlineData(2026, 3, 31, 17, 2026, 3, 31, 18, "es-ES", "Ocurre el día 31 de cada mes", "Cada 1 hora", "18:00", "31/03/2026")]
    // Step 3: Query on 31/03 at 18:00 -> Falls on the same day at 19:00 (last hour of the day)
    [InlineData(2026, 3, 31, 18, 2026, 3, 31, 19, "en-US", "Occurs day 31 of every month", "Every 1 hour", "7:00 PM", "03-31-2026")]
    [InlineData(2026, 3, 31, 18, 2026, 3, 31, 19, "en-GB", "Occurs day 31 of every month", "Every 1 hour", "19:00", "31/03/2026")]
    [InlineData(2026, 3, 31, 18, 2026, 3, 31, 19, "es-ES", "Ocurre el día 31 de cada mes", "Cada 1 hora", "19:00", "31/03/2026")]
    public void CalculateNextExecution_WhenSpecificDayDoesNotExistWithDailyFrequency_FindsNextExecution(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(19, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (15/02/2026 10:00) -> Jumps to May (31/05/2026 at 17:00)
    [InlineData(2026, 2, 15, 10, 2026, 5, 31, 17, "en-US", "Occurs day 31 of every 3 months", "Every 1 hour", "5:00 PM", "05-31-2026")]
    [InlineData(2026, 2, 15, 10, 2026, 5, 31, 17, "en-GB", "Occurs day 31 of every 3 months", "Every 1 hour", "17:00", "31/05/2026")]
    [InlineData(2026, 2, 15, 10, 2026, 5, 31, 17, "es-ES", "Ocurre el día 31 de cada 3 meses", "Cada 1 hora", "17:00", "31/05/2026")]
    // Step 2: Query on 31/05 at 17:00 -> Falls on the same day at 18:00
    [InlineData(2026, 5, 31, 17, 2026, 5, 31, 18, "en-US", "Occurs day 31 of every 3 months", "Every 1 hour", "6:00 PM", "05-31-2026")]
    [InlineData(2026, 5, 31, 17, 2026, 5, 31, 18, "en-GB", "Occurs day 31 of every 3 months", "Every 1 hour", "18:00", "31/05/2026")]
    [InlineData(2026, 5, 31, 17, 2026, 5, 31, 18, "es-ES", "Ocurre el día 31 de cada 3 meses", "Cada 1 hora", "18:00", "31/05/2026")]
    // Step 3: Query on 31/05 at 18:00 -> Falls on the same day at 19:00 (last hour of the day)
    [InlineData(2026, 5, 31, 18, 2026, 5, 31, 19, "en-US", "Occurs day 31 of every 3 months", "Every 1 hour", "7:00 PM", "05-31-2026")]
    [InlineData(2026, 5, 31, 18, 2026, 5, 31, 19, "en-GB", "Occurs day 31 of every 3 months", "Every 1 hour", "19:00", "31/05/2026")]
    [InlineData(2026, 5, 31, 18, 2026, 5, 31, 19, "es-ES", "Ocurre el día 31 de cada 3 meses", "Cada 1 hora", "19:00", "31/05/2026")]
    public void CalculateNextExecution_WhenSpecificDayDoesNotExistWithRecurrence_SkipsCorrectMonths(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 3, // Cada 3 meses
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 31
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(19, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 15 of every month", "10:00 AM", "06-15-2026")]
    [InlineData("en-GB", "Occurs day 15 of every month", "10:00", "15/06/2026")]
    [InlineData("es-ES", "Ocurre el día 15 de cada mes", "10:00", "15/06/2026")]
    public void CalculateNextExecution_WhenOccursOnceTimeHasPassed_ReturnsNextMonth(
           string locale,
           string expectedPrefix,
           string expectedTime,
           string expectedDate )
    {
        // Arrange: Check on May 15th at 4:00 PM, looking for the execution from 10:00 AM that day (already past)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 15, 16, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Should move to June 15th at 10:00 UTC
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime!.Value.Year.ShouldBe(2026);
        result.NextExecutionTime!.Value.Month.ShouldBe(6);
        result.NextExecutionTime!.Value.Day.ShouldBe(15);
        result.NextExecutionTime!.Value.Hour.ShouldBe(10);
        result.NextExecutionTime!.Value.Minute.ShouldBe(0);

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 29 of every month", "10:00 AM", "03-29-2025")]
    [InlineData("en-GB", "Occurs day 29 of every month", "10:00", "29/03/2025")]
    [InlineData("es-ES", "Ocurre el día 29 de cada mes", "10:00", "29/03/2025")]
    public void CalculateNextExecution_WhenDay29InNonLeapYear_SkipsFebruary(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Today is January 30th, 2025 (non-leap year). Configuration: Day 29 of every month.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 1, 30, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 29
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: February 2025 does not have day 29. March 2025 does have day 29.
        // Should skip February and calculate March 29th, 2025 at 10:00 UTC.
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime!.Value.Year.ShouldBe(2025);
        result.NextExecutionTime!.Value.Month.ShouldBe(3);
        result.NextExecutionTime!.Value.Day.ShouldBe(29);
        result.NextExecutionTime!.Value.Hour.ShouldBe(10);
        result.NextExecutionTime!.Value.Minute.ShouldBe(0);

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 10 of every month", "Every 2 hours", "8:00 AM", "05-10-2026")]
    [InlineData("en-GB", "Occurs day 10 of every month", "Every 2 hours", "08:00", "10/05/2026")]
    [InlineData("es-ES", "Ocurre el día 10 de cada mes", "Cada 2 horas", "08:00", "10/05/2026")]
    public void CalculateNextExecution_WhenTimeZoneIsProvided_ConvertsCorrectly(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate)
    {
        // Arrange: UTC time with conversion to CST/CDT time zone (UTC-5 in May 2026)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0)
            },
            TimeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The date engine should correctly calculate the local time zone offset
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the CDT offset (UTC-5 in May, which is numerically greater than -6)
        result.NextExecutionTime.Value.Offset.TotalHours.ShouldBeGreaterThan(-6);

        // Verify that the generated description applies the translations, frequencies, and local formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/04/2026 00:00) -> Last weekend day of April (Sunday 26/04 at 00:00)
    [InlineData(2026, 4, 1, 0, 2026, 4, 26, 0, "en-US", "Occurs the last weekend day of every month", "12:00 AM", "04-26-2026")]
    [InlineData(2026, 4, 1, 0, 2026, 4, 26, 0, "en-GB", "Occurs the last weekend day of every month", "00:00", "26/04/2026")]
    [InlineData(2026, 4, 1, 0, 2026, 4, 26, 0, "es-ES", "Ocurre el último día de fin de semana de cada mes", "00:00", "26/04/2026")]
    // Step 2: Query on Monday 27/04 at 00:00 -> Jumps to May (Sunday 31/05 at 00:00)
    [InlineData(2026, 4, 27, 0, 2026, 5, 31, 0, "en-US", "Occurs the last weekend day of every month", "12:00 AM", "05-31-2026")]
    [InlineData(2026, 4, 27, 0, 2026, 5, 31, 0, "en-GB", "Occurs the last weekend day of every month", "00:00", "31/05/2026")]
    [InlineData(2026, 4, 27, 0, 2026, 5, 31, 0, "es-ES", "Ocurre el último día de fin de semana de cada mes", "00:00", "31/05/2026")]
    public void CalculateNextExecution_WhenLastWeekendDayVariesByMonth_ReturnsCorrectExecution(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Last,
                RelativeDayType = MonthlyRelativeDayType.WeekendDay
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the strict match of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // Verify that the generated description applies the translations and corresponding formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Edge Cases & Boundaries

    #region Time Units Handling

    [Theory]
    // Step 1: Initial query (01/05/2026 10:00) -> Falls on May 10th at the first hour (08:00 AM)
    [InlineData(2026, 5, 1, 10, 0, 2026, 5, 10, 8, 0, "en-US", "Occurs day 10 of every month", "Every 15 minutes", "8:00 AM", "05-10-2026")]
    [InlineData(2026, 5, 1, 10, 0, 2026, 5, 10, 8, 0, "en-GB", "Occurs day 10 of every month", "Every 15 minutes", "08:00", "10/05/2026")]
    [InlineData(2026, 5, 1, 10, 0, 2026, 5, 10, 8, 0, "es-ES", "Ocurre el día 10 de cada mes", "Cada 15 minutos", "08:00", "10/05/2026")]
    // Step 2: Query on May 10th at 08:00 AM -> Falls on the same day at 08:15 AM
    [InlineData(2026, 5, 10, 8, 0, 2026, 5, 10, 8, 15, "en-US", "Occurs day 10 of every month", "Every 15 minutes", "8:15 AM", "05-10-2026")]
    [InlineData(2026, 5, 10, 8, 0, 2026, 5, 10, 8, 15, "en-GB", "Occurs day 10 of every month", "Every 15 minutes", "08:15", "10/05/2026")]
    [InlineData(2026, 5, 10, 8, 0, 2026, 5, 10, 8, 15, "es-ES", "Ocurre el día 10 de cada mes", "Cada 15 minutos", "08:15", "10/05/2026")]
    // Step 3: Query on May 10th at 08:15 AM -> Falls on the same day at 08:30 AM
    [InlineData(2026, 5, 10, 8, 15, 2026, 5, 10, 8, 30, "en-US", "Occurs day 10 of every month", "Every 15 minutes", "8:30 AM", "05-10-2026")]
    [InlineData(2026, 5, 10, 8, 15, 2026, 5, 10, 8, 30, "en-GB", "Occurs day 10 of every month", "Every 15 minutes", "08:30", "10/05/2026")]
    [InlineData(2026, 5, 10, 8, 15, 2026, 5, 10, 8, 30, "es-ES", "Ocurre el día 10 de cada mes", "Cada 15 minutos", "08:30", "10/05/2026")]
    // Step 4: Query on May 10th at 08:30 AM -> Falls on the same day at 08:45 AM
    [InlineData(2026, 5, 10, 8, 30, 2026, 5, 10, 8, 45, "en-US", "Occurs day 10 of every month", "Every 15 minutes", "8:45 AM", "05-10-2026")]
    [InlineData(2026, 5, 10, 8, 30, 2026, 5, 10, 8, 45, "en-GB", "Occurs day 10 of every month", "Every 15 minutes", "08:45", "10/05/2026")]
    [InlineData(2026, 5, 10, 8, 30, 2026, 5, 10, 8, 45, "es-ES", "Ocurre el día 10 de cada mes", "Cada 15 minutos", "08:45", "10/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryWithMinutes_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int queryMinute,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            int expectedMinute,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, queryMinute, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Minutes,
                FrequencyInterval = 15,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(9, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // strictly verify the consistency of the date and time returned by the engine.
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, TimeSpan.Zero));

        // verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/05/2026 10:00) -> Second Monday of May (11/05/2026 at 06:00)
    [InlineData(2026, 5, 1, 10, 2026, 5, 11, 6, "en-US", "Occurs the second monday of every month", "Every 6 hours", "6:00 AM", "05-11-2026")]
    [InlineData(2026, 5, 1, 10, 2026, 5, 11, 6, "en-GB", "Occurs the second monday of every month", "Every 6 hours", "06:00", "11/05/2026")]
    [InlineData(2026, 5, 1, 10, 2026, 5, 11, 6, "es-ES", "Ocurre el segundo lunes de cada mes", "Cada 6 horas", "06:00", "11/05/2026")]
    // Step 2: Query on Monday 11/05 at 06:00 -> Falls on the same day at 12:00 (noon)
    [InlineData(2026, 5, 11, 6, 2026, 5, 11, 12, "en-US", "Occurs the second monday of every month", "Every 6 hours", "12:00 PM", "05-11-2026")]
    [InlineData(2026, 5, 11, 6, 2026, 5, 11, 12, "en-GB", "Occurs the second monday of every month", "Every 6 hours", "12:00", "11/05/2026")]
    [InlineData(2026, 5, 11, 6, 2026, 5, 11, 12, "es-ES", "Ocurre el segundo lunes de cada mes", "Cada 6 horas", "12:00", "11/05/2026")]
    // Step 3: Query on Monday 11/05 at 12:00 -> Falls on the same day at 18:00 (evening)
    [InlineData(2026, 5, 11, 12, 2026, 5, 11, 18, "en-US", "Occurs the second monday of every month", "Every 6 hours", "6:00 PM", "05-11-2026")]
    [InlineData(2026, 5, 11, 12, 2026, 5, 11, 18, "en-GB", "Occurs the second monday of every month", "Every 6 hours", "18:00", "11/05/2026")]
    [InlineData(2026, 5, 11, 12, 2026, 5, 11, 18, "es-ES", "Ocurre el segundo lunes de cada mes", "Cada 6 horas", "18:00", "11/05/2026")]
    // Step 4: May day exhausted (query at 18:00) -> Jumps to the second Monday of June (08/06/2026 at 06:00)
    [InlineData(2026, 5, 11, 18, 2026, 6, 8, 6, "en-US", "Occurs the second monday of every month", "Every 6 hours", "6:00 AM", "06-08-2026")]
    [InlineData(2026, 5, 11, 18, 2026, 6, 8, 6, "en-GB", "Occurs the second monday of every month", "Every 6 hours", "06:00", "08/06/2026")]
    [InlineData(2026, 5, 11, 18, 2026, 6, 8, 6, "es-ES", "Ocurre el segundo lunes de cada mes", "Cada 6 horas", "06:00", "08/06/2026")]
    public void CalculateNextExecution_WhenOccursEveryWithHoursOnRelativeDay_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Second,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 6,
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(18, 0)
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // strictly verify the consistency of the date and time returned by the engine
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, 0, 0, TimeSpan.Zero));

        // verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Step 1: Initial query (01/05/2026 10:00:00) -> Falls on May 5th at the first hour (10:00:00 AM)
    [InlineData(2026, 5, 1, 10, 0, 0, 2026, 5, 5, 10, 0, 0, "en-US", "Occurs day 5 of every month", "Every 30 seconds", "10:00 AM", "05-05-2026")]
    [InlineData(2026, 5, 1, 10, 0, 0, 2026, 5, 5, 10, 0, 0, "en-GB", "Occurs day 5 of every month", "Every 30 seconds", "10:00", "05/05/2026")]
    [InlineData(2026, 5, 1, 10, 0, 0, 2026, 5, 5, 10, 0, 0, "es-ES", "Ocurre el día 5 de cada mes", "Cada 30 segundos", "10:00", "05/05/2026")]
    // Step 2: Query on May 5th at 10:00:00 AM -> Falls on the same day at 10:00:30 AM
    [InlineData(2026, 5, 5, 10, 0, 0, 2026, 5, 5, 10, 0, 30, "en-US", "Occurs day 5 of every month", "Every 30 seconds", "10:00 AM", "05-05-2026")]
    [InlineData(2026, 5, 5, 10, 0, 0, 2026, 5, 5, 10, 0, 30, "en-GB", "Occurs day 5 of every month", "Every 30 seconds", "10:00", "05/05/2026")]
    [InlineData(2026, 5, 5, 10, 0, 0, 2026, 5, 5, 10, 0, 30, "es-ES", "Ocurre el día 5 de cada mes", "Cada 30 segundos", "10:00", "05/05/2026")]
    // Step 3: Query on May 5th at 10:00:30 AM -> Falls on the same day at 10:01:00 AM
    [InlineData(2026, 5, 5, 10, 0, 30, 2026, 5, 5, 10, 1, 0, "en-US", "Occurs day 5 of every month", "Every 30 seconds", "10:01 AM", "05-05-2026")]
    [InlineData(2026, 5, 5, 10, 0, 30, 2026, 5, 5, 10, 1, 0, "en-GB", "Occurs day 5 of every month", "Every 30 seconds", "10:01", "05/05/2026")]
    [InlineData(2026, 5, 5, 10, 0, 30, 2026, 5, 5, 10, 1, 0, "es-ES", "Ocurre el día 5 de cada mes", "Cada 30 segundos", "10:01", "05/05/2026")]
    // Step 4: Query on May 5th at 10:01:00 AM -> Falls on the same day at 10:01:30 AM
    [InlineData(2026, 5, 5, 10, 1, 0, 2026, 5, 5, 10, 1, 30, "en-US", "Occurs day 5 of every month", "Every 30 seconds", "10:01 AM", "05-05-2026")]
    [InlineData(2026, 5, 5, 10, 1, 0, 2026, 5, 5, 10, 1, 30, "en-GB", "Occurs day 5 of every month", "Every 30 seconds", "10:01", "05/05/2026")]
    [InlineData(2026, 5, 5, 10, 1, 0, 2026, 5, 5, 10, 1, 30, "es-ES", "Ocurre el día 5 de cada mes", "Cada 30 segundos", "10:01", "05/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryWithSeconds_FollowsCorrectSequence(
            int queryYear,
            int queryMonth,
            int queryDay,
            int queryHour,
            int queryMinute,
            int querySecond,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            int expectedMinute,
            int expectedSecond,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(queryYear, queryMonth, queryDay, queryHour, queryMinute, querySecond, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 5
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Seconds,
                FrequencyInterval = 30,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 2)
            },
            LimitsStartDateLocal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // strictly verify the consistency of the date and time returned by the engine.
        result.NextExecutionTime.ShouldBe(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, TimeSpan.Zero));

        // verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Time Units Handling

    #region Description & Localization

    [Theory]
    // Case 1: Every month (1 month) - Specific day 10
    [InlineData(1, true, 10, null, null, "en-US", "every month")]
    [InlineData(1, true, 10, null, null, "en-GB", "every month")]
    [InlineData(1, true, 10, null, null, "es-ES", "cada mes")]
    // Case 2: Every 2 months - First Monday relative
    [InlineData(2, false, null, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Monday, "en-US", "every 2 months")]
    [InlineData(2, false, null, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Monday, "en-GB", "every 2 months")]
    [InlineData(2, false, null, MonthlyRelativeOrdinal.First, MonthlyRelativeDayType.Monday, "es-ES", "cada 2 meses")]
    public void CalculateNextExecution_WhenMonthlyPattern_GeneratesCorrectDescription(
            int recursEvery,
            bool isSpecificDay,
            int? specificDayNumber,
            MonthlyRelativeOrdinal? relativeOrdinal,
            MonthlyRelativeDayType? relativeDayType,
            string locale,
            string expectedDescriptionSubstring )
    {
        // Arrange - Deterministic and localized coverage for monthly recurrence patterns
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = recursEvery,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = isSpecificDay,
                SpecificDayNumber = specificDayNumber,
                RelativeOrdinal = relativeOrdinal,
                RelativeDayType = relativeDayType
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that the description contains the corresponding localized interval
        result.Description.ShouldContain(expectedDescriptionSubstring);
    }


    [Theory]
    [InlineData("en-US", "Every 3 hours")]
    [InlineData("en-GB", "Every 3 hours")]
    [InlineData("es-ES", "Cada 3 horas")]
    public void CalculateNextExecution_WhenOccursEvery_GeneratesCorrectDescription(
            string locale,
            string expectedFrequency )
    {
        // Arrange - Deterministic and localized coverage for monthly periodic frequencies
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 15
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 3,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The description should include the localized intraday frequency details
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that the description contains the localized intraday frequency interval
        result.Description.ShouldContain(expectedFrequency);
    }


    [Theory]
    [InlineData("en-US", "Occurs the first monday of every 2 months", "3:00 PM", "08-03-2026")]
    [InlineData("en-GB", "Occurs the first monday of every 2 months", "15:00", "03/08/2026")]
    [InlineData("es-ES", "Ocurre el primer lunes de cada 2 meses", "15:00", "03/08/2026")]
    public void CalculateNextExecution_WhenOccursOnce_GeneratesCorrectDescription(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 2,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.First,
                RelativeDayType = MonthlyRelativeDayType.Monday
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The description should detail the localized pattern
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the correct 2-month jump (Monday, August 3, 2026) at 15:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(8);
        result.NextExecutionTime.Value.Day.ShouldBe(3);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);

        // Verify that the description contains the expected pattern and localized time and date format
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs day 10 of every month", "2:30 PM", "05-10-2026")]
    [InlineData("en-GB", "Occurs day 10 of every month", "14:30", "10/05/2026")]
    [InlineData("es-ES", "Ocurre el día 10 de cada mes", "14:30", "10/05/2026")]
    public void CalculateNextExecution_WhenOccursOnce_DescriptionIncludesTime(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Query on May 1, 2026, at 10:00 AM. Next execution: May 10, 2026.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = true,
                SpecificDayNumber = 10
            },
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(14, 30)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The description should include the localized time, date, and expected pattern
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();

        // Verify the localized elements within the output string
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs the fourth thursday of every month", "10:00 AM", "06-25-2026")]
    [InlineData("en-GB", "Occurs the fourth thursday of every month", "10:00", "25/06/2026")]
    [InlineData("es-ES", "Ocurre el cuarto jueves de cada mes", "10:00", "25/06/2026")]
    public void CalculateNextExecution_WhenRelativeDayWithSpecificDayOfWeek_GeneratesCorrectDescription(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange - Deterministic and localized coverage for specific monthly weekdays (fourth Thursday)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), // Tuesday, June 2, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Monthly,
            RecursEvery = 1,
            MonthlyConfiguration = new()
            {
                IsSpecificDay = false,
                RelativeOrdinal = MonthlyRelativeOrdinal.Fourth,
                RelativeDayType = MonthlyRelativeDayType.Thursday
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: The date engine should correctly calculate the fourth Thursday of June 2026
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(6);
        result.NextExecutionTime.Value.Day.ShouldBe(25);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.NextExecutionTime.Value.DayOfWeek.ShouldBe(DayOfWeek.Thursday);

        // Verify that the generated description applies the correct translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Description & Localization

}
