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

    #endregion

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

    #endregion

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

    #endregion

}