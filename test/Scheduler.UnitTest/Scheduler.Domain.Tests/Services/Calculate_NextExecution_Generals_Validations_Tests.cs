using Scheduler.Domain.Models;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Services;

public class Calculate_NextExecution_Generals_Validations_Tests
{
    private readonly SchedulerService _service;

    public Calculate_NextExecution_Generals_Validations_Tests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Calculate_NextExecution_Generals_WithNullConfig_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = _service.CalculateNextExecution(currentDate, null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("The configuration cannot be null.");
    }

    [Fact]
    public void Calculate_NextExecution_Generals_WhenStrategyCombinationIsNotRegistered_ReturnsUnsupportedCombinationError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        var schedulerService = new SchedulerService(new ISchedulerStrategy[]
        {
            new OnceDailySchedulerStrategy()
        });

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            TimeZoneId = TimeZoneInfo.Utc.Id,
            Locale = "en-US",
        };

        // Act
        var result = schedulerService.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Unsupported schedule and occurs combination.");
    }

    [Fact]
    public void Calculate_NextExecution_Generals_WithInvalidTimeZone_ReturnsError()
    {
        // Arrange
        var currentDate = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        SchedulerConfiguration config = new()
        {
            Enabled = true,
            Type = SchedulerType.Recurring,
            RecursEvery = 1,
            Occurs = OccursType.Daily,
            TimeZoneId = "Invalid/Zone_Name",
            Locale = "en-US",
        };

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        // We use ShouldStartWith because the message usually includes the invalid ID
        result.ErrorMessage.ShouldStartWith("Invalid TimeZoneId");
    }

}