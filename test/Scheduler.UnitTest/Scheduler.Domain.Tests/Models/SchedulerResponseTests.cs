//using Scheduler.Domain.Models;
//using Shouldly;

//namespace Scheduler.Domain.Tests.Models;

//public class SchedulerResponseTests
//{
//    [Fact]
//    public void SchedulerResponse_Unsorted_Executions_Should_Be_Sorted_Chronologically()
//    {
//        // Arrange
//        var earliestDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
//        var laterDate = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
//        var unsortedList = new List<DateTimeOffset> { laterDate, earliestDate };

//        // Act
//        var response = new SchedulerResponse(unsortedList, "Sorting test");

//        // Assert
//        response.IsSuccess.ShouldBeTrue();
//        response.NextExecutionTime.ShouldBe(earliestDate);
//        response.NextExecutionTimes.First().ShouldBe(earliestDate);
//        response.NextExecutionTimes.Count().ShouldBe(2);
//        response.ErrorMessage.ShouldBeEmpty();
//    }

//    [Fact]
//    public void SchedulerResponse_Motive_The_Constructor_Should_The_Input_Error_Message()
//    {
//        // Arrange
//        var expectedMessage = "The builder interprets the single message sent as an error message.";

//        // Act
//        var response = new SchedulerResponse(expectedMessage);

//        // Assert
//        response.IsSuccess.ShouldBeFalse();
//        response.NextExecutionTime.ShouldBeNull();
//        response.NextExecutionTimes.ShouldBeEmpty();
//        response.ErrorMessage.ShouldBe(expectedMessage);
//    }

//    [Fact]
//    public void SchedulerResponse_Constructor_Should_Be_NullSafe_When_Given_Null_Executions()
//    {
//        // Act
//        var response = new SchedulerResponse(executions: null!, description: "Null test");

//        // Assert
//        response.IsSuccess.ShouldBeTrue();
//        response.NextExecutionTimes.ShouldNotBeNull();
//        response.NextExecutionTimes.ShouldBeEmpty();
//        response.NextExecutionTime.ShouldBeNull();
//    }

//    [Fact]
//    public void SchedulerResponse_Default_State_Should_Be_Safe_And_Null()
//    {
//        // Act
//        SchedulerResponse response = default;

//        // Assert
//        response.IsSuccess.ShouldBeFalse();
//        response.NextExecutionTime.ShouldBeNull();
//        response.NextExecutionTimes.ShouldBeNull();
//    }

//}