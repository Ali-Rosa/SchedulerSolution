using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;
using Shouldly;

namespace Scheduler.Domain.Tests.Validators;

public class ScheduleConfigurationValidatorTests
{
    private readonly SchedulerService _service;

    public ScheduleConfigurationValidatorTests() => _service = SchedulerServiceFactory.CreateDefault();

    [Fact]
    public void Negative_RecursEvery_Should_Be_Rejected()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_RecursEvery(-1)
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The Every value cannot be negative.");
        result.NextExecutionTime.ShouldBeNull();
    }

    [Fact]
    public void Undefined_ScheduleType_Should_Return_Definition_Error()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Invalid_ScheduleType()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Not defined schedule type.");
    }

    [Fact]
    public void Disabled_Configuration_Should_Return_Disabled_Message()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .Disabled()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("The schedule is disabled.");
    }

    [Fact]
    public void Undefined_OccursType_Should_Return_Definition_Error()
    {
        // Arrange
        var config = ScheduleConfigurationBuilder.OnceDaily()
            .With_Invalid_OccursType()
            .Build();

        // Act
        var result = _service.CalculateNextExecution(DateTimeOffset.UtcNow, config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Not defined occurs type.");
    }
}