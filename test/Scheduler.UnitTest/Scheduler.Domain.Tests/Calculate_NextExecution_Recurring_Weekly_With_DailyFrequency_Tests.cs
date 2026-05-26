//using Scheduler.Domain.Models;
//using Scheduler.Domain.Models.Daily;
//using Scheduler.Domain.Services;
//using Scheduler.Domain.Strategies;
//using Shouldly;

//namespace Scheduler.Domain.Tests;

//public class Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Tests
//{
//    private readonly SchedulerService _service = new([new RecurringWeeklySchedulerStrategy()]);

//    #region Sequence Progression

//    [Theory]
//    // Stage: Wednesday, January 1, 2020. Days: Mon, Wed, Fri. Hours: 4, 6, 8 AM.
//    [InlineData(5, 1, 6)]  // It's 5 AM -> Next is today at 6 AM
//    [InlineData(7, 1, 8)]  // It's 7 AM -> Next is today at 8 AM
//    [InlineData(22, 3, 4)] // It's 10 PM -> Next is Friday 03 at 4 AM
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_IntraDay_Sequence_Should_Navigate_Correct_Moments(int currentHour, int expectedDay, int expectedHour)
//    {
//        // Arrange
//        var currentDate = new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new() 
//            { 
//                OccursEveryEnable = true, 
//                IntervalUnit = TimeIntervalUnit.Hours,
//                FrequencyInterval = 2,
//                StartTime = new TimeOnly(4, 0), 
//                EndTime = new TimeOnly(8, 0) 
//            },
//            WeeklyConfiguration = new() 
//            { 
//                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday] 
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(expectedDay);
//        result.NextExecutionTime.Value.Hour.ShouldBe(expectedHour);
//    }

//    #endregion Sequence Progression

//    #region Complex Week Jumping

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Should_Jump_Multiple_Weeks_Forward_Correctly()
//    {
//        // Friday, January 3, 2020 at 10 PM. Pattern: Every 2 weeks, Monday/Friday.
//        var currentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 2,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Hours,
//                FrequencyInterval = 2,
//                StartTime = new TimeOnly(4, 0),
//                EndTime = new TimeOnly(8, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
//            },
//            FirstDayOfWeek = DayOfWeek.Monday,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Week 0 ends. Week 1 is skipped. Week 2 starts on Monday 13 at 4 AM.
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(13);
//        result.NextExecutionTime.Value.Hour.ShouldBe(4);
//    }

//    #endregion Complex Week Jumping


//    #region Limits & Safety

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Should_Respect_EndDate_Limit_ESTA_PRUEBA_ES_SIN_DAILY_FREACUENCY()
//    {
//        var currentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
//            LimitsEndDateLocal = currentDate.AddDays(4), // End date is before the next Monday (6th Jan)
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };


//        var result = _service.CalculateNextExecution(currentDate, config);

//        result.IsSuccess.ShouldBeFalse();
//        result.ErrorMessage.ShouldContain("No valid executions were found within the limits with this configuration.");
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Should_Handle_Null_DaysOfWeek_Safely()
//    {
//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            WeeklyConfiguration = new() { DaysOfWeek = null! },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

//        result.IsSuccess.ShouldBeFalse();
//        result.ErrorMessage.ShouldContain("Weekly configuration");
//    }

//    #endregion Limits & Safety

//    #region OccursOnce Mode

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursOnce_Weekly_Should_Execute_At_Specified_Time_On_Valid_Day()
//    {
//        // Arrange: Wednesday at 10 AM, execution at 3 PM same day
//        var currentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = false,
//                OnceTime = new TimeOnly(15, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(1);
//        result.NextExecutionTime.Value.Hour.ShouldBe(15);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursOnce_Weekly_Should_Skip_To_Next_Valid_Day_If_Time_Passed()
//    {
//        // Arrange: Wednesday at 10 PM, execution at 3 PM already passed
//        var currentDate = new DateTimeOffset(2020, 1, 1, 22, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = false,
//                OnceTime = new TimeOnly(15, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(3); // Friday
//        result.NextExecutionTime.Value.Hour.ShouldBe(15);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursOnce_Weekly_Should_Jump_Weeks_When_No_Valid_Day_In_Current_Week()
//    {
//        // Arrange: Friday at 10 PM, pattern only Tuesday/Wednesday, every 1 week
//        var currentDate = new DateTimeOffset(2020, 1, 3, 22, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = false,
//                OnceTime = new TimeOnly(9, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Wednesday]
//            },
//            FirstDayOfWeek = DayOfWeek.Monday,
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Day.ShouldBe(7); // Tuesday of next week
//        result.NextExecutionTime.Value.Hour.ShouldBe(9);
//    }

//    #endregion OccursOnce Mode

//    #region OccursEvery Mode - Different Intervals

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursEvery_Minutes_Should_Generate_Multiple_Executions()
//    {
//        // Arrange: Wednesday 4:55 AM, pattern every 15 minutes from 4 AM to 5 AM
//        var currentDate = new DateTimeOffset(2020, 1, 1, 4, 55, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Minutes,
//                FrequencyInterval = 15,
//                StartTime = new TimeOnly(4, 0),
//                EndTime = new TimeOnly(5, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Hour.ShouldBe(5);
//        result.NextExecutionTime.Value.Minute.ShouldBe(0);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursEvery_Seconds_Should_Find_Granular_Intervals()
//    {
//        // Arrange: Wednesday 4:00:50, pattern every 20 seconds from 4 AM to 4:01 AM
//        var currentDate = new DateTimeOffset(2020, 1, 1, 4, 0, 50, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Seconds,
//                FrequencyInterval = 20,
//                StartTime = new TimeOnly(4, 0, 0),
//                EndTime = new TimeOnly(4, 1, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Hour.ShouldBe(4);
//        result.NextExecutionTime.Value.Minute.ShouldBe(1);
//        result.NextExecutionTime.Value.Second.ShouldBe(0);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursEvery_Seconds_Should_Find_Granular_Intervals_Specific()
//    {
//        // Arrange: Wednesday 4:00:30, pattern every 20 seconds from 4 AM to 4:01 AM
//        var currentDate = new DateTimeOffset(2020, 1, 1, 4, 0, 30, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Seconds,
//                FrequencyInterval = 20,
//                StartTime = new TimeOnly(4, 0, 0),
//                EndTime = new TimeOnly(4, 1, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//        result.NextExecutionTime.Value.Hour.ShouldBe(4);
//        result.NextExecutionTime.Value.Minute.ShouldBe(0);
//        result.NextExecutionTime.Value.Second.ShouldBe(40); // 4:00:30 -> next is 4:00:40
//    }

//    [Theory]
//    [InlineData(1)]  // Every 1 hour: 5 AM is valid
//    [InlineData(2)]  // Every 2 hours: 6 AM is valid (4 + 2)
//    [InlineData(4)]  // Every 4 hours: 4 AM is valid (start), next would be 8 AM
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_OccursEvery_Hours_Should_Handle_Various_Intervals(int hourInterval)
//    {
//        // Arrange: Wednesday 5 AM, pattern every N hours from 4 AM to 8 AM
//        var currentDate = new DateTimeOffset(2020, 1, 1, 5, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Hours,
//                FrequencyInterval = hourInterval,
//                StartTime = new TimeOnly(4, 0),
//                EndTime = new TimeOnly(8, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();

//        // Validate that the result is within the expected range and respects the interval
//        var nextHour = result.NextExecutionTime.Value.Hour;
//        nextHour.ShouldBeGreaterThanOrEqualTo(4);  // Cannot be before start (4 AM)
//        nextHour.ShouldBeLessThanOrEqualTo(8);     // Cannot be after end (8 AM)
//    }

//    [Fact]
//    public void CalculateNextExecution_RecurringWeekly_DailyFrequencyCrossingMidnight_ReturnsNextDayExecution()
//    {
//        // Arrange
//        var currentDate = new DateTimeOffset(2020, 1, 1, 23, 0, 0, TimeSpan.Zero); // Wednesday 11:00 PM

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = true,
//                IntervalUnit = TimeIntervalUnit.Hours,
//                FrequencyInterval = 1,
//                StartTime = new TimeOnly(22, 0), // 10 PM
//                EndTime = new TimeOnly(23, 59, 59) // Until end of day
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Thursday]
//            },
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();

//        // Since it's 11:00 PM, the 11:00 PM task is not included (e > now).
//        // The next available is Thursday (day 2) at 10:00 PM.
//        var nextExecution = result.NextExecutionTime.Value;
//        nextExecution.Day.ShouldBe(2);
//        nextExecution.Hour.ShouldBe(22);
//    }

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Should_Return_Error_When_No_Executions_Within_Start_Date_And_End_Date()
//    {
//        // Arrange: Pattern is Monday/Friday, but we're past end date before any execution
//        var currentDate = new DateTimeOffset(2020, 1, 8, 10, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = false,
//                OnceTime = new TimeOnly(9, 0)
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday]
//            },
//            LimitsStartDateLocal = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
//            LimitsEndDateLocal = new DateTimeOffset(2020, 1, 7, 23, 59, 59, TimeSpan.Zero),
//            TimeZoneId = TimeZoneInfo.Utc.Id,
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeFalse();
//    }

//    #endregion Edge Cases & Boundary Conditions

//    #region Time Zone Handling

//    [Fact]
//    public void Calculate_NextExecution_Recurring_Weekly_With_DailyFrequency_Should_Respect_Time_Zone_When_Calculating_Next_Execution()
//    {
//        // Arrange: Different time zone (e.g., America/New_York)
//        var currentDate = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

//        SchedulerConfiguration config = new()
//        {
//            Enabled = true,
//            Type = SchedulerType.Recurring,
//            Occurs = OccursType.Weekly,
//            RecursEvery = 1,
//            DailyFrequencyConfiguration = new()
//            {
//                OccursEveryEnable = false,
//                OnceTime = new TimeOnly(15, 0) // 3 PM in New York
//            },
//            WeeklyConfiguration = new()
//            {
//                DaysOfWeek = [DayOfWeek.Wednesday]
//            },
//            TimeZoneId = "America/New_York",
//            Locale = "en-US",
//        };

//        // Act
//        var result = _service.CalculateNextExecution(currentDate, config);

//        // Assert
//        result.IsSuccess.ShouldBeTrue();
//        result.NextExecutionTime.ShouldNotBeNull();
//    }

//    #endregion Time Zone Handling

//}