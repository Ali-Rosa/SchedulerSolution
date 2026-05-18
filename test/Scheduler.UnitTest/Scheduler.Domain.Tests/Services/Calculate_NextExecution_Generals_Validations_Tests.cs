using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Tests.TestHelpers.Builders;
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

        // We manually created a service with only one strategy to force a search failure
        var schedulerService = new SchedulerService(new ISchedulerStrategy[]
        {
            new OnceDailySchedulerStrategy()
        });

        // We attempt to request a RecurringDaily that is not registered in this schedulerService
        var config = ScheduleConfigurationBuilder.RecurringDaily().With_Locale("en-US").Build();

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
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Locale("en-US")
            .With_TimeZoneId("Invalid/Zone_Name")
            .Build();

        // Act
        var result = _service.CalculateNextExecution(currentDate, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.NextExecutionTime.ShouldBeNull();
        // We use ShouldStartWith because the message usually includes the invalid ID
        result.ErrorMessage.ShouldStartWith("Invalid TimeZoneId");
    }

}