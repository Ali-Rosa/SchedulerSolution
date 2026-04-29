using Scheduler.Domain.Services;
using Scheduler.Domain.Tests.TestHelpers.Builders;
using Scheduler.Domain.Tests.TestHelpers.Factories;


namespace Scheduler.Domain.Tests.Services;

public class CalculateNextExecution_WeeklyScheduleTest
{
    private readonly SchedulerService _service;

    public CalculateNextExecution_WeeklyScheduleTest()
    {
        _service = SchedulerServiceFactory.CreateDefault();
    }

    [Fact]
    public void WeeklySchedule_WithIntraDay_ReturnsFirstValidThursday()
    {
        // Arrange

        var startDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero); // Wednesday


    }





}
