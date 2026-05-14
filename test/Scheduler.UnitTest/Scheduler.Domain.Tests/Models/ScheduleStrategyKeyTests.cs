using Scheduler.Domain.Models;
using Shouldly;

namespace Scheduler.Domain.Tests.Models;

public class ScheduleStrategyKeyTests
{
    [Fact]
    public void Key_Should_Follow_Value_Equality_Contract()
    {
        // Arrange
        var key1 = new SchedulerStrategyKey(SchedulerType.Recurring, SchedulerOccursType.Daily);
        var key2 = new SchedulerStrategyKey(SchedulerType.Recurring, SchedulerOccursType.Daily);
        var key3 = new SchedulerStrategyKey(SchedulerType.Once, SchedulerOccursType.Daily);

        // Assert: Equality & Operators (Shouldly handles the structural equality of records)
        key1.ShouldBe(key2);
        (key1 == key2).ShouldBeTrue();
        (key1 != key3).ShouldBeTrue();
        key1.Equals((object)key2).ShouldBeTrue(); // Validation of equality with boxing

        // Assert: Properties Access
        key1.ScheduleType.ShouldBe(SchedulerType.Recurring);
        key1.OccursType.ShouldBe(SchedulerOccursType.Daily);

        // Assert: HashCode Consistency
        key1.GetHashCode().ShouldBe(key2.GetHashCode());
        key1.GetHashCode().ShouldNotBe(key3.GetHashCode());

        // Assert
        var stringRepresentation = key1.ToString();
        stringRepresentation.ShouldNotBeNullOrEmpty();
        stringRepresentation.ShouldContain(nameof(SchedulerType.Recurring));
        stringRepresentation.ShouldContain(nameof(SchedulerOccursType.Daily));
    }

}