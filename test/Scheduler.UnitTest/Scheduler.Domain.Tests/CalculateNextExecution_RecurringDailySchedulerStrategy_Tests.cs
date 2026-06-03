using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_RecurringDailySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new RecurringDailySchedulerStrategy()]);

    #region Specific validations for strategy

    
    [Theory]
    [InlineData("en-US", "The frequency interval must be greater than 0.")]
    [InlineData("en-GB", "The frequency interval must be greater than 0.")]
    [InlineData("es-ES", "El intervalo de frecuencia debe ser mayor que 0.")]
    public void CalculateNextExecution_WhenDailyFrequencyIntervalIsNegative_ReturnsValidationError(
        string locale, 
        string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = -1,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        result.ErrorMessage.ShouldContain(expectedErrorMessage);
    }


    [Theory]
    [InlineData("en-US", "Not defined interval unit for daily frequency.")]
    [InlineData("en-GB", "Not defined interval unit for daily frequency.")]
    [InlineData("es-ES", "Unidad de intervalo no definida para la frecuencia diaria.")]
    public void CalculateNextExecution_WhenDailyFrequencyIntervalUnitIsUndefined_ReturnsValidationError(
        string locale, 
        string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = (TimeIntervalUnit)1981,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        result.ErrorMessage.ShouldContain(expectedErrorMessage);
    }


    #endregion Specific validations for strategy

    #region Mode Selection (Once vs Every)


    [Theory]
    [InlineData("en-US", "Occurs every day", "3:00 PM", "05-06-2026")]
    [InlineData("en-GB", "Occurs every day", "15:00", "06/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "15:00", "06/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryIsDisabled_OnceTimeIsEnabledAndIgnoresIntervalConfiguration(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }

    
    #endregion Mode Selection (Once vs Every)

    #region Mode: Occurs Once


    [Theory]
    [InlineData("en-US", "Occurs every day", "3:00 PM", "05-06-2026")]
    [InlineData("en-GB", "Occurs every day", "15:00", "06/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "15:00", "06/05/2026")]
    public void CalculateNextExecution_WhenOnceModeAndTimeIsInFuture_ReturnsSameDayExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 10 AM current, execution planned for 3 PM today.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(15, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every day", "8:00 AM", "05-07-2026")]
    [InlineData("en-GB", "Occurs every day", "08:00", "07/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "08:00", "07/05/2026")]
    public void CalculateNextExecution_WhenOnceModeAndTimeHasPassed_ReturnsNextValidDay(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 10 PM now, execution planned for 8 AM. Should jump to tomorrow.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OnceTime = new TimeOnly(8, 0),
                OccursEveryEnable = false,
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it moves to the next day (day 7) at 08:00 UTC
        result.NextExecutionTime.Value.Day.ShouldBe(7);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Mode: Occurs Once

    #region Mode: Occurs Every


    [Theory]
    [InlineData("en-US", "Occurs every day", "Every 2 hours", "6:00 AM", "05-06-2026")]
    [InlineData("en-GB", "Occurs every day", "Every 2 hours", "06:00", "06/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "Cada 2 horas", "06:00", "06/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryAndNextIntervalExists_ReturnsNextInterval(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 5 AM now. Planned hours: 4, 6, 8 AM. Next interval: 6 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it moves to the next valid interval (6:00 AM) on the same day (day 6)
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every 3 days", "Every 2 hours", "4:00 AM", "05-09-2026")]
    [InlineData("en-GB", "Occurs every 3 days", "Every 2 hours", "04:00", "09/05/2026")]
    [InlineData("es-ES", "Ocurre cada 3 días", "Cada 2 horas", "04:00", "09/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryAndDayIsExhausted_ReturnsNextPatternDay(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 10 PM now. Pattern every 3 days. Schedule 4-8 AM. Next: Day 09 at 4 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 3,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the correct jump of 3 days (May 9) at its first interval (04:00 AM)
        result.NextExecutionTime.Value.Day.ShouldBe(9);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every 3 days", "Every 2 hours", "6:00 AM", "05-06-2026")]
    [InlineData("en-GB", "Occurs every 3 days", "Every 2 hours", "06:00", "06/05/2026")]
    [InlineData("es-ES", "Ocurre cada 3 días.", "Cada 2 horas", "06:00", "06/05/2026")]
    public void Calculate_NextExecution_RecurringDailyEveryDay_Should_Return_Sequence(
        string locale,
        string expectedPrefix,
        string expectedFrequency,
        string expectedTime,
        string expectedDate)
    {
        // Arrange: 5 AM now. Recurs every day. Daily frequency every 2 hours from 4:00 to 8:00.
        // Since 4:00 AM has passed for May 6th, the sequence starts at 6:00 AM.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 3,  // every 3 days
            Occurs = OccursType.Daily,
            MaxOccurrences = 10, // We limit the sequence to 10 actions
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Expected sequence: 
        // - May 6: Only 6:00 AM and 8:00 AM are in the future.
        // - May 9: 4:00 AM, 6:00 AM, and 8:00 AM.
        // - May 12: 4:00 AM, 6:00 AM, and 8:00 AM.
        // - May 15: 4:00 AM, 6:00 AM.
        var expectedSequence = new List<DateTimeOffset>
    {
        new(2026, 5, 6, 6, 0, 0, TimeSpan.Zero),
        new(2026, 5, 6, 8, 0, 0, TimeSpan.Zero),
        new(2026, 5, 9, 4, 0, 0, TimeSpan.Zero),
        new(2026, 5, 9, 6, 0, 0, TimeSpan.Zero),
        new(2026, 5, 9, 8, 0, 0, TimeSpan.Zero),
        new(2026, 5, 12, 4, 0, 0, TimeSpan.Zero),
        new(2026, 5, 12, 6, 0, 0, TimeSpan.Zero),
        new(2026, 5, 12, 8, 0, 0, TimeSpan.Zero),
        new(2026, 5, 15, 4, 0, 0, TimeSpan.Zero),
        new(2026, 5, 15, 6, 0, 0, TimeSpan.Zero)
    };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTimes.ShouldBe(expectedSequence);
        // Validate that the description of the first execution matches
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }

    
    #endregion Mode: Occurs Every

    #region Calendar Pattern (RecursEvery / Days)


    [Theory]
    [InlineData("en-US", "Occurs every 3 days", "12:00 PM", "05-04-2026")]
    [InlineData("en-GB", "Occurs every 3 days", "12:00", "04/05/2026")]
    [InlineData("es-ES", "Ocurre cada 3 días", "12:00", "04/05/2026")]
    public void CalculateNextExecution_WhenRecurringEveryDays_SkipsDaysCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Day 01 + pattern every 3 days = Day 04 of May
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 3,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it jumps exactly 3 days (from May 1 to May 4) at 12:00 UTC
        result.NextExecutionTime.Value.Day.ShouldBe(4);
        result.NextExecutionTime.Value.Hour.ShouldBe(12);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Calendar Pattern (RecursEvery / Days)

    #region Anchor & Default Behavior

    
    [Theory]
    [InlineData("en-US", "Occurs every day", "10:30 AM", "05-02-2026")]
    [InlineData("en-GB", "Occurs every day", "10:30", "02/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "10:30", "02/05/2026")]
    public void CalculateNextExecution_WhenNoDailyFrequency_UsesCurrentTimeAsAnchorAndMovesToNextDay(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Request at 10:30 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), // is ignored
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 10:30 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(30);
        // Verify that the description contains the localized time and date format
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Anchor & Default Behavior

    #region Limits Handling

    [Theory]
    [InlineData("en-US", "Occurs every day", "10:00 AM", "05-10-2026")]
    [InlineData("en-GB", "Occurs every day", "10:00", "10/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "10:00", "10/05/2026")]
    public void CalculateNextExecution_WhenStartLimitIsInFuture_AdjustsExecutionCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Request on day 01 at 10 AM. Start limit on day 10.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero).AddDays(9),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it moves exactly to the start limit date (May 10) at 10:00 UTC
        result.NextExecutionTime.Value.Day.ShouldBe(10);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "No valid executions were found within the limits with this configuration.")]
    [InlineData("en-GB", "No valid executions were found within the limits with this configuration.")]
    [InlineData("es-ES", "No se encontraron ejecuciones válidas dentro de los límites con esta configuración.")]
    public void CalculateNextExecution_WhenEndLimitPreventsExecution_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange: Today is day 01. Every 10 days (next would be day 11). The limit is day 05.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 10,
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero).AddDays(4),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        // Verify that the error message is translated correctly according to the locale
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }



    #endregion Limits Handling  

    #region Edge Cases & Boundaries

    
    [Theory]
    [InlineData("en-US", "Occurs every day", "Every 2 hours", "6:00 AM", "05-06-2026")]
    [InlineData("en-GB", "Occurs every day", "Every 2 hours", "06:00", "06/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "Cada 2 horas", "06:00", "06/05/2026")]
    public void CalculateNextExecution_WhenCurrentTimeEqualsStartTime_ReturnsNextInterval(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Scenario: If the request is exactly at 04:00:00, the rule 'e > now' should move to the 06:00 interval.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 4, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it jumps to the next interval (06:00 AM) due to the strict 'e > now' rule
        result.NextExecutionTime.Value.Day.ShouldBe(6);
        result.NextExecutionTime.Value.Hour.ShouldBe(6);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every day", "10:00 PM", "01-01-2026")]
    [InlineData("en-GB", "Occurs every day", "22:00", "01/01/2026")]
    [InlineData("es-ES", "Ocurre cada día", "22:00", "01/01/2026")]
    public void CalculateNextExecution_WhenCrossingYearBoundary_ReturnsNextDayCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 31 Dec -> 1 Jan of the next year
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the year boundary crossing to January 1, 2026
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(22);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every day", "10:00 AM", "02-29-2024")]
    [InlineData("en-GB", "Occurs every day", "10:00", "29/02/2024")]
    [InlineData("es-ES", "Ocurre cada día", "10:00", "29/02/2024")]
    public void CalculateNextExecution_WhenLeapYearOccurs_HandlesFebruary29Correctly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that the leap year is handled correctly (February 29, 2024)
        result.NextExecutionTime.Value.Year.ShouldBe(2024);
        result.NextExecutionTime.Value.Month.ShouldBe(2);
        result.NextExecutionTime.Value.Day.ShouldBe(29);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Edge Cases & Boundaries

    #region Time Units Handling

    
    [Theory]
    // Cases of 15 Minutes
    [InlineData(TimeIntervalUnit.Minutes, 15, 12, 15, 0, "en-US", "Occurs every day", "Every 15 minutes", "12:15 PM", "05-06-2026")]
    [InlineData(TimeIntervalUnit.Minutes, 15, 12, 15, 0, "en-GB", "Occurs every day", "Every 15 minutes", "12:15", "06/05/2026")]
    [InlineData(TimeIntervalUnit.Minutes, 15, 12, 15, 0, "es-ES", "Ocurre cada día", "Cada 15 minutos", "12:15", "06/05/2026")]
    // Cases of 20 Seconds
    [InlineData(TimeIntervalUnit.Seconds, 20, 12, 0, 20, "en-US", "Occurs every day", "Every 20 seconds", "12:00 PM", "05-06-2026")]
    [InlineData(TimeIntervalUnit.Seconds, 20, 12, 0, 20, "en-GB", "Occurs every day", "Every 20 seconds", "12:00", "06/05/2026")]
    [InlineData(TimeIntervalUnit.Seconds, 20, 12, 0, 20, "es-ES", "Ocurre cada día", "Cada 20 segundos", "12:00", "06/05/2026")]
    public void CalculateNextExecution_WhenOccursEveryWithSmallUnits_ReturnsCorrectExecution(
            TimeIntervalUnit unit,
            int interval,
            int h,
            int m,
            int s,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: 12:00:00
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = unit,
                FrequencyInterval = interval,
                StartTime = new TimeOnly(12, 0, 0),
                EndTime = new TimeOnly(13, 0, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the temporal accuracy at the small unit level
        result.NextExecutionTime.Value.Hour.ShouldBe(h);
        result.NextExecutionTime.Value.Minute.ShouldBe(m);
        result.NextExecutionTime.Value.Second.ShouldBe(s);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Time Units Handling

    #region Description & Localization


    [Theory]
    [InlineData("en-US", "Every 2 hours", "at 4:00 AM")]
    [InlineData("en-GB", "Every 2 hours", "at 04:00")]
    [InlineData("es-ES", "Cada 2 horas", "a las 04:00")]
    public void CalculateNextExecution_WhenOccursEvery_GeneratesDetailedDescription(
            string locale,
            string expectedFrequency,
            string expectedTime )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Verify in a localized manner that it contains the expected fragments
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
    }


    [Theory]
    // Cases of interval 1 (Occurs every day)
    [InlineData(1, "en-US", "Occurs every day")]
    [InlineData(1, "en-GB", "Occurs every day")]
    [InlineData(1, "es-ES", "Ocurre cada día")]
    // Cases of interval 3 (Occurs every 3 days)
    [InlineData(3, "en-US", "Occurs every 3 days")]
    [InlineData(3, "en-GB", "Occurs every 3 days")]
    [InlineData(3, "es-ES", "Ocurre cada 3 días")]
    public void CalculateNextExecution_WhenRecurringPattern_DescriptionReflectsInterval(
            int every,
            string locale,
            string expectedPrefix )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = every,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Verify that the prefix of the recurrence text is correctly translated
        result.Description.ShouldContain(expectedPrefix);
    }


    #endregion Description & Localization

    #region Logical Consistency


    [Theory]
    [InlineData("en-US", "Occurs every day", "8:00 AM", "05-02-2026")]
    [InlineData("en-GB", "Occurs every day", "08:00", "02/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "08:00", "02/05/2026")]
    public void CalculateNextExecution_WhenNoDailyFrequency_IgnoresExecutionDateTimeLocal(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Request at 8 AM, configured at 11 PM (but should be ignored)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero).AddHours(15), // 11 PM, but should be ignored
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Tomorrow at 8 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(8);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description is in the requested language format
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every day", "10:00 AM", "05-02-2026")]
    [InlineData("en-GB", "Occurs every day", "10:00", "02/05/2026")]
    [InlineData("es-ES", "Ocurre cada día", "10:00", "02/05/2026")]
    public void CalculateNextExecution_WhenFirstDayOfWeekChanges_DoesNotAffectDailyCalculation(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Daily behavior should be identical regardless of whether the week starts on Monday or Sunday
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var resMonday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Monday });
        var resSunday = _service.CalculateNextExecution(config with { FirstDayOfWeek = DayOfWeek.Sunday });

        // Assert
        resMonday.IsSuccess.ShouldBeTrue();
        resSunday.IsSuccess.ShouldBeTrue();

        // Verify logical equality: weekly changes do not affect the daily time series
        resMonday.NextExecutionTime.ShouldBe(resSunday.NextExecutionTime);
        // verify that the descriptive text remains identical and adapted to the language
        resMonday.Description.ShouldBe(resSunday.Description);
        resMonday.Description.ShouldContain(expectedPrefix);
        resMonday.Description.ShouldContain(expectedTime);
        resMonday.Description.ShouldContain(expectedDate);
    }


    #endregion Logical Consistency

    #region Date Sequences

    
    [Theory]
    [InlineData("en-US", "Occurs every 2 days", "Every 2 hours", "4:00 AM", "01-01-2020")]
    [InlineData("en-GB", "Occurs every 2 days", "Every 2 hours", "04:00", "01/01/2020")]
    [InlineData("es-ES", "Ocurre cada 2 días", "Cada 2 horas", "04:00", "01/01/2020")]
    public void Calculate_NextExecution_RecurringDaily_Should_Return_Sequence_In_Single_Call(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Occurs every 2 days, daily frequency every 2 hours from 4:00 to 8:00, starting on 01/01/2020.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Daily,
            RecursEvery = 2,
            MaxOccurrences = 6,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Expected sequence of executions (the mathematical logic is identical in all languages)
        var expectedSequence = new List<DateTimeOffset>
        {
            new(2020, 1, 1, 4, 0, 0, TimeSpan.Zero),
            new(2020, 1, 1, 6, 0, 0, TimeSpan.Zero),
            new(2020, 1, 1, 8, 0, 0, TimeSpan.Zero),
            new(2020, 1, 3, 4, 0, 0, TimeSpan.Zero),
            new(2020, 1, 3, 6, 0, 0, TimeSpan.Zero),
            new(2020, 1, 3, 8, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Validate that the complete sequence of executions matches exactly
        result.NextExecutionTimes.ShouldBe(expectedSequence);

        // Validate that the description of the first execution reflects the requested language
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }

    
    #endregion Date Sequences

}