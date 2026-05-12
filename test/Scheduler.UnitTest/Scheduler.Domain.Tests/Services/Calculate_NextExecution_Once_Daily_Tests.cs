using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Once_Daily_Tests
{
    private readonly SchedulerService _service;
    public Calculate_NextExecution_Once_Daily_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    #region Basic & Default Scenarios

    [Fact]
    public void CurrentDate_Should_Be_Used_When_No_Execution_Time_Provided()
    {
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily().With_Locale("en-US").Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(currentDate);
        result.Description.ShouldContain("Occurs once");
    }

    [Fact]
    public void Exact_CurrentDate_Should_Be_Valid_Execution_Time()
    {
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(currentDate)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(currentDate);
    }

    [Fact]
    public void ExecutionDateTimeLocal_Should_Be_Used_As_NextExecution()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var currentDate = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(executionDateTime);
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("15/05/2026");
        result.Description.ShouldContain("14:30");
    }

    [Fact]
    public void Without_ExecutionDateTimeLocal_Should_Use_CurrentDate()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(currentDate);
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("12/05/2026");
        result.Description.ShouldContain("10:00");
    }

    #endregion Basic & Default Scenarios

    #region Execution Limits

    [Theory]
    [InlineData(1, "DateTime cannot be less than CurrentDate")] // 1 hour ago
    public void Past_Execution_Dates_Should_Return_Specific_Error(int hoursBack, string expectedError)
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var pastExecution = currentDate.AddHours(-hoursBack);

        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(pastExecution)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Theory]
    [InlineData(10, 5)]  // Execution in 10 days, Limit in 5 (Exceeds end)
    [InlineData(2, 5)]  // Execution in 2 days, Start Limit in 5 days (Fails for being before start)
    public void Dates_Outside_Start_Or_End_Limits_Should_Fail(int daysToExecution, int daysToLimit)
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var execution = currentDate.AddDays(daysToExecution);

        var builder = ScheduleConfigurationBuilder.OnceDaily().With_Locale("en-US").With_ExecutionDateTimeLocal(execution);

        // If daysToExecution is less than the limit, test StartDate
        if (daysToExecution < daysToLimit)
            builder.With_Limits_StartDateLocal(currentDate.AddDays(daysToLimit));
        else // If greater, test EndDate
            builder.With_Limits_EndDateLocal(currentDate.AddDays(daysToLimit));

        // Act
        var result = _service.CalculateNextExecution(currentDate, builder.Build());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The execution date is outside the allowed range.");
    }

    #endregion Execution Limits

    #region Date/Time Range Limits

    [Fact]
    public void ExecutionDateTimeLocal_Before_LimitsStartDateLocal_Should_Fail()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero); // Before execution
        var startLimit = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The execution date is outside the allowed range.");
    }

    [Fact]
    public void ExecutionDateTimeLocal_After_LimitsEndDateLocal_Should_Fail()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero); // Before execution
        var endLimit = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The execution date is outside the allowed range.");
    }

    [Fact]
    public void ExecutionDateTimeLocal_Within_Valid_Range_Should_Succeed()
    {
        // Arrange
        var startLimit = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        var endLimit = new DateTimeOffset(2026, 5, 20, 23, 59, 59, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(executionDateTime);
    }

    [Fact]
    public void Boundary_Execution_At_StartDate_Should_Succeed()
    {
        // Arrange: Execution exactly at the start date boundary
        var startLimit = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(executionDateTime);
    }

    [Fact]
    public void Boundary_Execution_At_EndDate_Should_Succeed()
    {
        // Arrange: Execution exactly at the end date boundary
        var endLimit = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_EndDateLocal(endLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(executionDateTime);
    }

    #endregion Date/Time Range Limits

    #region TimeZone & Localization

    [Fact]
    public void Response_Should_Correctly_Convert_To_Local_Time()
    {
        // Arrange: 10:00 AM UTC. Madrid in May is UTC+2.
        var currentDateUtc = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var timeZoneId = "Romance Standard Time";

        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_TimeZoneId(timeZoneId)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDateUtc, config);

        // Assert
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Hour.ShouldBe(12); // 10 + 2
        result.NextExecutionTime.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Description_Should_Include_Starting_Label_When_Limit_Is_Present()
    {
        var currentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var startLimit = currentDate.AddDays(-1);

        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        var result = _service.CalculateNextExecution(currentDate, config);

        result.Description.ShouldContain("starting on");
        result.Description.ShouldContain(startLimit.ToString("dd/MM/yyyy"));
    }

    #endregion TimeZone & Localization

    #region Current Date Comparison

    [Fact]
    public void ExecutionDateTime_Equal_To_CurrentDate_Should_Use_ExecutionDateTime()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(dateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(dateTime, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Date.ShouldBe(dateTime.Date);
        result.NextExecutionTime.Value.Hour.ShouldBe(dateTime.Hour);
        result.NextExecutionTime.Value.Minute.ShouldBe(dateTime.Minute);
    }

    [Fact]
    public void ExecutionDateTime_Before_CurrentDate_Should_Fail()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("DateTime cannot be less than CurrentDate");
    }

    [Fact]
    public void ExecutionDateTime_After_CurrentDate_Should_Use_ExecutionDateTime()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        result.NextExecutionTime.Value.Date.ShouldBe(executionDateTime.Date);
        result.NextExecutionTime.Value.Hour.ShouldBe(executionDateTime.Hour);
        result.NextExecutionTime.Value.Minute.ShouldBe(executionDateTime.Minute);
    }

    #endregion Current Date Comparison

    #region Description Formatting

    [Fact]
    public void Description_Should_Include_StartDate_When_LimitsStartDateLocal_Exists()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var startLimit = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_Limits_StartDateLocal(startLimit)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldContain("Occurs once");
        result.Description.ShouldContain("15/05/2026");
        result.Description.ShouldContain("starting on 10/05/2026");
    }

    [Fact]
    public void Description_Should_Not_Include_StartDate_When_LimitsStartDateLocal_Is_Null()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldNotContain("starting on");
    }

    [Fact]
    public void Description_Should_Respect_TimeZone_Conversion()
    {
        // Arrange: 10:00 UTC in CST (UTC-6 in winter) = 04:00 CST
        var executionDateTime = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var currentDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero); // Before execution

        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_TimeZoneId(cstZone.Id)
            .With_ExecutionDateTimeLocal(executionDateTime)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldNotBeNull();
        // The result should be converted to CST time (10:00 UTC - 6 hours = 04:00 CST)
        result.NextExecutionTime.Value.Hour.ShouldBe(4);
        result.NextExecutionTime.Value.Minute.ShouldBe(0);
        // The description should show the local time (4:00 AM)
        result.Description.ShouldContain("04:00");
    }

    #endregion Description Formatting

    #region Logical Consistency

    [Fact]
    public void RecursEvery_Should_Be_Ignored_For_Once_Schedule()
    {
        // Arrange: RecursEvery value should be irrelevant
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_RecursEvery(5) // Should be ignored
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.NextExecutionTime.ShouldBe(executionDateTime);
    }

    [Fact]
    public void DailyFrecuency_Should_Be_Ignored_For_Once_Schedule()
    {
        // Arrange
        var executionDateTime = new DateTimeOffset(2026, 5, 15, 14, 30, 0, TimeSpan.Zero);
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_ExecutionDateTimeLocal(executionDateTime)
            .With_DailyFrecuency_OccursEvery(TimeIntervalUnit.Hours, 2, new TimeOnly(8, 0), new TimeOnly(18, 0))
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert: Only one execution, not multiple
        result.IsSuccess.ShouldBeTrue();
        result.NextsExecutionsTimes.Count().ShouldBe(1);
        result.NextExecutionTime.ShouldBe(executionDateTime);
    }

    #endregion Logical Consistency

}