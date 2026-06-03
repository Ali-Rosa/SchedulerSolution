using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests;

public class CalculateNextExecution_RecurringWeeklySchedulerStrategy_Tests
{
    private readonly SchedulerService _service = new([new RecurringWeeklySchedulerStrategy()]);

    #region Specific validations for strategy

    
    [Theory]
    [InlineData("en-US", "Weekly configuration is required for Weekly recurring schedules.")]
    [InlineData("en-GB", "Weekly configuration is required for Weekly recurring schedules.")]
    [InlineData("es-ES", "La configuración semanal es requerida para planificaciones recurrentes semanales.")]
    public void CalculateNextExecution_WhenWeeklyConfigurationIsNull_ReturnsValidationError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        // Verify that the validation error message is returned in the corresponding language
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    [Theory]
    [InlineData(null, "en-US", "Weekly configuration requires at least one day.")]
    [InlineData(null, "en-GB", "Weekly configuration requires at least one day.")]
    [InlineData(null, "es-ES", "La configuración semanal requiere al menos un día.")]
    [InlineData(new object[] { new DayOfWeek[0], "en-US", "Weekly configuration requires at least one day." })]
    [InlineData(new object[] { new DayOfWeek[0], "en-GB", "Weekly configuration requires at least one day." })]
    [InlineData(new object[] { new DayOfWeek[0], "es-ES", "La configuración semanal requiere al menos un día." })]
    public void CalculateNextExecution_WhenWeeklyDaysAreNullOrEmpty_ReturnsValidationError(
            DayOfWeek[]? daysOfWeek,
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = DateTimeOffset.UtcNow,
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = daysOfWeek! },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        // Verify that the validation error message is returned in the corresponding language
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    #endregion Specific validations for strategy

    #region OccursOnce Mode


    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday", "3:00 PM", "01-01-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday", "15:00", "01/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles", "15:00", "01/01/2020")]
    public void CalculateNextExecution_WhenOccursOnceAndValidDay_ReturnsSameDayExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Wednesday at 10 AM, execution planned for 3 PM on the same day
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that the execution result occurs on the same day (Wednesday, January 1, 2020) at 15:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday and Friday", "3:00 PM", "01-03-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday and Friday", "15:00", "03/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles y viernes", "15:00", "03/01/2020")]
    public void CalculateNextExecution_WhenOccursOnceAndTimeHasPassed_ReturnsNextValidDay(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Wednesday at 10 PM, execution planned for 3 PM today has already passed
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that it moves to Friday (January 3, 2020) at 15:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(3); // Friday
        result.NextExecutionTime.Value.Hour.ShouldBe(15);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every week on Tuesday and Wednesday", "9:00 AM", "01-07-2020")]
    [InlineData("en-GB", "Occurs every week on Tuesday and Wednesday", "09:00", "07/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el martes y miércoles", "09:00", "07/01/2020")]
    public void CalculateNextExecution_WhenOccursOnceAndNoValidDayInCurrentWeek_JumpsToNextWeek(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Friday at 10 PM, configured days Tuesday/Wednesday, every 1 week.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Wednesday]
            },
            FirstDayOfWeek = DayOfWeek.Monday,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify that it jumps to Tuesday of the next week (January 7, 2020) at 09:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(7); // Tuesday of the next week
        result.NextExecutionTime.Value.Hour.ShouldBe(9);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every 2 weeks on Wednesday and Friday", "3:00 PM", "01-03-2020")]
    [InlineData("en-GB", "Occurs every 2 weeks on Wednesday and Friday", "15:00", "03/01/2020")]
    [InlineData("es-ES", "Ocurre cada 2 semanas el miércoles y viernes", "15:00", "03/01/2020")]
    public void Calculate_NextExecution_RecurringBiWeekly_Should_Return_Sequence(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Start on Wednesday, Jan 1, 2020 at 10:00 PM. 
        // RecursEvery = 2 (every other week on Wednesday and Friday).
        // Week of Jan 1 is active (Wednesday has passed, Friday runs).
        // Week of Jan 5 is skipped.
        // Week of Jan 12 is active (Wednesday and Friday run).
        // Week of Jan 19 is skipped.
        // Week of Jan 26 is active (Wednesday and Friday run).
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2, // Ejecución cada dos semanas
            MaxOccurrences = 5,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Expected sequence of executions (alternating weeks):
        // 1. Friday, Jan 3, 2020
        // -- Week of Jan 5 is skipped --
        // 2. Wednesday, Jan 15, 2020
        // 3. Friday, Jan 17, 2020
        // -- Week of Jan 19 is skipped --
        // 4. Wednesday, Jan 29, 2020
        // 5. Friday, Jan 31, 2020
        var expectedSequence = new List<DateTimeOffset>
        {
            new(2020, 1, 3, 15, 0, 0, TimeSpan.Zero),
            new(2020, 1, 15, 15, 0, 0, TimeSpan.Zero),
            new(2020, 1, 17, 15, 0, 0, TimeSpan.Zero),
            new(2020, 1, 29, 15, 0, 0, TimeSpan.Zero),
            new(2020, 1, 31, 15, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Validate that the bi-weekly sequence matches exactly
        result.NextExecutionTimes.ShouldBe(expectedSequence);
        // Validate that the description of the first execution reflects the language and the 2-week interval
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }



    #endregion OccursOnce Mode

    #region OccursEvery Mode


    [Theory]
    // Case 1: Query 5 AM -> Next is today at 6 AM
    [InlineData(5, 1, 6, "en-US", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "6:00 AM", "01-01-2020")]
    [InlineData(5, 1, 6, "en-GB", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "06:00", "01/01/2020")]
    [InlineData(5, 1, 6, "es-ES", "Ocurre cada semana el lunes, miércoles y viernes", "Cada 2 horas", "06:00", "01/01/2020")]
    // Case 2: Query 7 AM -> Next is today at 8 AM
    [InlineData(7, 1, 8, "en-US", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "8:00 AM", "01-01-2020")]
    [InlineData(7, 1, 8, "en-GB", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "08:00", "01/01/2020")]
    [InlineData(7, 1, 8, "es-ES", "Ocurre cada semana el lunes, miércoles y viernes", "Cada 2 horas", "08:00", "01/01/2020")]
    // Case 3: Query 10 PM (22h) -> Next is Friday 03 at 4 AM
    [InlineData(22, 3, 4, "en-US", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "4:00 AM", "01-03-2020")]
    [InlineData(22, 3, 4, "en-GB", "Occurs every week on Monday, Wednesday and Friday", "Every 2 hours", "04:00", "03/01/2020")]
    [InlineData(22, 3, 4, "es-ES", "Ocurre cada semana el lunes, miércoles y viernes", "Cada 2 horas", "04:00", "03/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryWithinSameDay_ReturnsNextInterval(
            int currentHour,
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
            CurrentDate = new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the logical accuracy of time jumps calculated by the engine
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion OccursEvery Mode

    #region Weekly Pattern (DaysOfWeek)


    [Theory]
    [InlineData("en-US", "Occurs every week on Friday", "10:00 AM", "05-08-2026")]
    [InlineData("en-GB", "Occurs every week on Friday", "10:00", "08/05/2026")]
    [InlineData("es-ES", "Ocurre cada semana el viernes", "10:00", "08/05/2026")]
    public void CalculateNextExecution_WhenNextValidDayExistsInSameWeek_ReturnsSameWeekExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Today is Tuesday 5th at 10:00 AM. Days: Friday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it remains in the same week (Friday, May 8, 2026) at 10:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(8);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Case 1: Monday, May 4th at 12:00 PM -> Should fall on Wednesday, May 6th at 12:00 AM (Same week)
    [InlineData("en-US", 0, 12, 6, "Occurs every 3 weeks on Monday, Wednesday and Friday", "12:00 AM", "05-06-2026")]
    [InlineData("en-GB", 0, 12, 6, "Occurs every 3 weeks on Monday, Wednesday and Friday", "00:00", "06/05/2026")]
    [InlineData("es-ES", 0, 12, 6, "Ocurre cada 3 semanas el lunes, miércoles y viernes", "00:00", "06/05/2026")]
    // Case 2: Friday, May 8th at 12:00 PM -> Skips weeks -> Should fall on Monday, May 25th at 12:00 AM (Week 3)
    [InlineData("en-US", 4, 12, 25, "Occurs every 3 weeks on Monday, Wednesday and Friday", "12:00 AM", "05-25-2026")]
    [InlineData("en-GB", 4, 12, 25, "Occurs every 3 weeks on Monday, Wednesday and Friday", "00:00", "25/05/2026")]
    [InlineData("es-ES", 4, 12, 25, "Ocurre cada 3 semanas el lunes, miércoles y viernes", "00:00", "25/05/2026")]
    public void CalculateNextExecution_WhenMultipleDaysConfigured_ReturnsCorrectNextExecution(
            string locale,
            int offsetDays,
            int offsetHours,
            int expectedDay,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the calculated week jumps by the engine
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        // Being anchored to the LimitsStartDateLocal time, the hour will always be 00:00
        result.NextExecutionTime.Value.Hour.ShouldBe(0);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Weekly Pattern (DaysOfWeek)

    #region Week Recurrence (RecursEvery)


    [Theory]
    [InlineData("en-US", "Occurs every 2 weeks on Monday", "12:01 AM", "05-18-2026")]
    [InlineData("en-GB", "Occurs every 2 weeks on Monday", "00:01", "18/05/2026")]
    [InlineData("es-ES", "Ocurre cada 2 semanas el lunes", "00:01", "18/05/2026")]
    public void CalculateNextExecution_WhenRecurringEveryWeeks_SkipsWeeksCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Monday, May 4th at 12:01 AM. Every 2 weeks, Monday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 1, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the correct 2-week jump (from Monday, May 4th to Monday, May 18th) at 12:01 AM UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(18);
        result.NextExecutionTime.Value.Hour.ShouldBe(0);
        result.NextExecutionTime.Value.Minute.ShouldBe(1);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every 2 weeks on Monday and Friday", "Every 2 hours", "4:00 AM", "01-13-2020")]
    [InlineData("en-GB", "Occurs every 2 weeks on Monday and Friday", "Every 2 hours", "04:00", "13/01/2020")]
    [InlineData("es-ES", "Ocurre cada 2 semanas el lunes y viernes", "Cada 2 horas", "04:00", "13/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryAndWeeksAreSkipped_JumpsCorrectNumberOfWeeks(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Friday, January 3rd, 2020 at 10 PM. Pattern: every 2 weeks, Monday/Friday.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 2,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
            },
            FirstDayOfWeek = DayOfWeek.Monday,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the correct week jump (Monday, January 13th, 2020) at 04:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(13);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Week Recurrence (RecursEvery)

    #region Anchor & Default Behavior


    [Theory]
    [InlineData("en-US", "Occurs every week on Monday", "10:00 AM", "05-11-2026")]
    [InlineData("en-GB", "Occurs every week on Monday", "10:00", "11/05/2026")]
    [InlineData("es-ES", "Ocurre cada semana el lunes", "10:00", "11/05/2026")]
    public void CalculateNextExecution_WhenNoDailyFrequency_UsesAnchorTimeAndIgnoresExecutionDateTimeLocal(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), // Monday the 4th at 10:00 AM.
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 20, 11, 0, 0, TimeSpan.Zero).AddHours(5), // 03:00 PM - Should be ignored
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert: Next Monday (day 11) at 10:00 AM
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(11);
        result.NextExecutionTime.Value.Hour.ShouldBe(10);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Anchor & Default Behavior

    #region Limits Handling


    [Theory]
    [InlineData("en-US", "No valid executions were found within the limits with this configuration.")]
    [InlineData("en-GB", "No valid executions were found within the limits with this configuration.")]
    [InlineData("es-ES", "No se encontraron ejecuciones válidas dentro de los límites con esta configuración.")]
    public void CalculateNextExecution_WhenEndLimitPreventsExecution_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] }, // The next Friday is 2026-05-08
            LimitsEndDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Before the next execution (Thursday 7)
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        // Verify that the validation error message is returned in the corresponding language
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    #endregion Limits Handling

    #region Edge Cases & Boundaries


    [Theory]
    [InlineData("en-US", "Occurs every 2 weeks on Monday", "12:00 AM", "01-05-2026")]
    [InlineData("en-GB", "Occurs every 2 weeks on Monday", "00:00", "05/01/2026")]
    [InlineData("es-ES", "Ocurre cada 2 semanas el lunes", "00:00", "05/01/2026")]
    public void CalculateNextExecution_WhenCrossingYearBoundary_ReturnsCorrectWeekExecution(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Monday, December 22, 2025 at 12:01 AM. 
        // Start limit: Monday, December 22 at 12:00 AM (midnight anchor).
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the year boundary crossing to Monday, January 5, 2026 at 12:00 AM UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(5);
        result.NextExecutionTime.Value.Hour.ShouldBe(0);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every 2 weeks on Thursday", "12:00 AM", "03-07-2024")]
    [InlineData("en-GB", "Occurs every 2 weeks on Thursday", "00:00", "07/03/2024")]
    [InlineData("es-ES", "Ocurre cada 2 semanas el jueves", "00:00", "07/03/2024")]
    public void CalculateNextExecution_WhenLeapYearOccurs_HandlesWeeklyCalculationCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Thursday, February 22, 2024 at 12:01 AM. 
        // Start limit: Thursday, February 22 at 12:00 AM (midnight anchor).
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
            Locale = locale
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it correctly moves considering the leap year to Thursday, March 7, 2024
        result.NextExecutionTime.Value.Year.ShouldBe(2024);
        result.NextExecutionTime.Value.Month.ShouldBe(3);
        result.NextExecutionTime.Value.Day.ShouldBe(7);
        result.NextExecutionTime.Value.Hour.ShouldBe(0);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday and Thursday", "Every 1 hour", "10:00 PM", "01-02-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday and Thursday", "Every 1 hour", "22:00", "02/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles y jueves", "Cada 1 hora", "22:00", "02/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryCrossesMidnight_ReturnsNextValidDayExecution(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 23, 0, 0, TimeSpan.Zero), // Wednesday 11:00 PM
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = 1,
                StartTime = new TimeOnly(22, 0), // 10 PM
                EndTime = new TimeOnly(23, 59, 59) // Until the end of the day
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Thursday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that it moves to Thursday (January 2, 2020) at 10:00 PM UTC (first interval of that day)
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(2);
        result.NextExecutionTime.Value.Hour.ShouldBe(22);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "No valid executions were found within the limits with this configuration.")]
    [InlineData("en-GB", "No valid executions were found within the limits with this configuration.")]
    [InlineData("es-ES", "No se encontraron ejecuciones válidas dentro de los límites con esta configuración.")]
    public void CalculateNextExecution_WhenNoExecutionsWithinLimits_ReturnsError(
            string locale,
            string expectedErrorMessage )
    {
        // Arrange: The pattern is Monday/Friday, but the end date has already passed before any execution.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 8, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(9, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
            },
            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LimitsEndDateLocal = new DateTimeOffset(2020, 1, 7, 23, 59, 59, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.Description.ShouldBeEmpty();
        // Verify that the validation error message is returned in the corresponding language
        result.ErrorMessage.ShouldBe(expectedErrorMessage);
    }


    #endregion Edge Cases & Boundaries

    #region Time Units Handling

    
    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday", "Every 15 minutes", "5:00 AM", "01-01-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday", "Every 15 minutes", "05:00", "01/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles", "Cada 15 minutos", "05:00", "01/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryWithMinutesInterval_ReturnsNextValidExecution(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Wednesday 4:55 AM, pattern of every 15 minutes from 4 AM to 5 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 4, 55, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Minutes,
                FrequencyInterval = 15,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(5, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the exact minute of execution (5:00 AM) on Wednesday, January 1, 2020
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(5);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday", "Every 20 seconds", "4:01 AM", "01-01-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday", "Every 20 seconds", "04:01", "01/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles", "Cada 20 segundos", "04:01", "01/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryWithSecondsInterval_ReturnsNextValidExecution(
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Wednesday 4:00:50, pattern of every 20 seconds from 4 AM to 4:01 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 4, 0, 50, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Seconds,
                FrequencyInterval = 20,
                StartTime = new TimeOnly(4, 0, 0),
                EndTime = new TimeOnly(4, 1, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the exact second of execution (4:01:00 AM) on Wednesday, January 1, 2020
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(1);
        result.NextExecutionTime.Value.Second.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    // Interval of 1 Hour (Next is at 6 AM)
    [InlineData(1, 6, "en-US", "Occurs every week on Wednesday", "Every 1 hour", "6:00 AM", "01-01-2020")]
    [InlineData(1, 6, "en-GB", "Occurs every week on Wednesday", "Every 1 hour", "06:00", "01/01/2020")]
    [InlineData(1, 6, "es-ES", "Ocurre cada semana el miércoles", "Cada 1 hora", "06:00", "01/01/2020")]
    // Interval of 2 Hours (Next is at 6 AM)
    [InlineData(2, 6, "en-US", "Occurs every week on Wednesday", "Every 2 hours", "6:00 AM", "01-01-2020")]
    [InlineData(2, 6, "en-GB", "Occurs every week on Wednesday", "Every 2 hours", "06:00", "01/01/2020")]
    [InlineData(2, 6, "es-ES", "Ocurre cada semana el miércoles", "Cada 2 horas", "06:00", "01/01/2020")]
    // Interval of 4 Hours (Next is at 8 AM)
    [InlineData(4, 8, "en-US", "Occurs every week on Wednesday", "Every 4 hours", "8:00 AM", "01-01-2020")]
    [InlineData(4, 8, "en-GB", "Occurs every week on Wednesday", "Every 4 hours", "08:00", "01/01/2020")]
    [InlineData(4, 8, "es-ES", "Ocurre cada semana el miércoles", "Cada 4 horas", "08:00", "01/01/2020")]
    public void CalculateNextExecution_WhenOccursEveryWithHourIntervals_ReturnsNextExecutionWithinValidInterval(
            int hourInterval,
            int expectedHour,
            string locale,
            string expectedPrefix,
            string expectedFrequency,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Wednesday 5 AM, pattern of every N hours from 4 AM to 8 AM
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 5, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = true,
                IntervalUnit = TimeIntervalUnit.Hours,
                FrequencyInterval = hourInterval,
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(8, 0)
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify the exact hour calculated by the engine
        result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the corresponding translations and formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedFrequency);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Time Units Handling

    #region Description & Localization

    [Theory]
    [InlineData("en-US", "Occurs every week on Friday", "10:00 AM", "05-08-2026")]
    [InlineData("en-GB", "Occurs every week on Friday", "10:00", "08/05/2026")]
    [InlineData("es-ES", "Ocurre cada semana el viernes", "10:00", "08/05/2026")]
    public void CalculateNextExecution_WhenLocaleIsDifferent_FormatsDescriptionAccordingly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Monday, May 4, 2026 at 10:00 AM. Next Friday is May 8.
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrEmpty();
        // Verify that the description applies the translations, time format, and date order of the requested locale
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    [Theory]
    [InlineData("en-US", "Occurs every week on Wednesday", "3:00 PM", "01-01-2020")]
    [InlineData("en-GB", "Occurs every week on Wednesday", "15:00", "01/01/2020")]
    [InlineData("es-ES", "Ocurre cada semana el miércoles", "15:00", "01/01/2020")]
    public void CalculateNextExecution_WhenTimeZoneIsProvided_ConvertsExecutionCorrectly(
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange: Different time zone (America/New_York)
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero),
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 1,
            DailyFrequencyConfiguration = new()
            {
                OccursEveryEnable = false,
                OnceTime = new TimeOnly(15, 0) // 3 PM in New York
            },
            WeeklyConfiguration = new()
            {
                DaysOfWeek = [DayOfWeek.Wednesday]
            },
            TimeZoneId = "America/New_York",
            Locale = locale,
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();

        // Verify the time zone conversion calculation: 
        // 15:00 EST (UTC-5 in January winter time) is equivalent to 20:00 UTC
        result.NextExecutionTime.Value.Year.ShouldBe(2020);
        result.NextExecutionTime.Value.Month.ShouldBe(1);
        result.NextExecutionTime.Value.Day.ShouldBe(1);
        result.NextExecutionTime.Value.Hour.ShouldBe(20);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the translations and local formats of New York
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Description & Localization

    #region Logical Consistency


    [Theory]
    // Case 1: Starts on Monday -> Monday 11 is Week 1 (Skipped) -> Falls on Monday 18 (Week 2)
    [InlineData(DayOfWeek.Monday, 18, "en-US", "Occurs every 2 weeks on Monday", "12:00 AM", "05-18-2026")]
    [InlineData(DayOfWeek.Monday, 18, "en-GB", "Occurs every 2 weeks on Monday", "00:00", "18/05/2026")]
    [InlineData(DayOfWeek.Monday, 18, "es-ES", "Ocurre cada 2 semanas el lunes", "00:00", "18/05/2026")]
    // Case 2: Starts on Thursday -> Monday 11 is Week 0 (Hit) -> Falls on Monday 11 (Week 0)
    [InlineData(DayOfWeek.Thursday, 11, "en-US", "Occurs every 2 weeks on Monday", "12:00 AM", "05-11-2026")]
    [InlineData(DayOfWeek.Thursday, 11, "en-GB", "Occurs every 2 weeks on Monday", "00:00", "11/05/2026")]
    [InlineData(DayOfWeek.Thursday, 11, "es-ES", "Ocurre cada 2 semanas el lunes", "00:00", "11/05/2026")]
    public void CalculateNextExecution_WhenFirstDayOfWeekChanges_AffectsWeekGrouping(
            DayOfWeek firstDayOfWeek,
            int expectedDay,
            string locale,
            string expectedPrefix,
            string expectedTime,
            string expectedDate )
    {
        // Arrange
        SchedulerConfiguration config = new()
        {
            CurrentDate = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero), // Thursday, May 7, 2026
            Enabled = true,
            Type = SchedulerType.Recurring,
            Occurs = OccursType.Weekly,
            RecursEvery = 2,
            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
            LimitsStartDateLocal = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = locale,
            FirstDayOfWeek = firstDayOfWeek
        };

        // Act
        var result = _service.CalculateNextExecution(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // Verify that the resulting date is correct according to the weekly grouping
        result.NextExecutionTime.Value.Year.ShouldBe(2026);
        result.NextExecutionTime.Value.Month.ShouldBe(5);
        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
        // Being anchored to the LimitsStartDateLocal time, the time will always be 00:00
        result.NextExecutionTime.Value.Hour.ShouldBe(0);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // Verify that the generated description applies the translations and local formats
        result.Description.ShouldContain(expectedPrefix);
        result.Description.ShouldContain(expectedTime);
        result.Description.ShouldContain(expectedDate);
    }


    #endregion Logical Consistency

}