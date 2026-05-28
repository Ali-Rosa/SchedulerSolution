using Scheduler.Domain.Models;
using Scheduler.Domain.Models.Daily;
using Scheduler.Domain.Services;
using Scheduler.Domain.Strategies;
using Shouldly;

namespace Scheduler.Domain.Tests
{
    public class CalculateNextExecution_RecurringWeeklySchedulerStrategy_Tests
    {
        private readonly SchedulerService _service = new([new RecurringWeeklySchedulerStrategy()]);

        #region Mode Selection (Once vs Every)
        #endregion Mode Selection (Once vs Every)

        #region OccursOnce Mode
        #endregion OccursOnce Mode

        #region OccursEvery Mode
        #endregion OccursEvery Mode

        #region Weekly Pattern (DaysOfWeek)

        [Fact]
        public void CalculateNextExecution_WhenNextValidDayExistsInSameWeek_ReturnsSameWeekExecution()
        {
            // Today Tuesday 05 at 10:00 AM. Days: Friday.
            SchedulerConfiguration config = new()
            {
                CurrentDate = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero),
                Enabled = true,
                Type = SchedulerType.Recurring,
                Occurs = OccursType.Weekly,
                RecursEvery = 1,
                WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Friday] },
                TimeZoneId = TimeZoneInfo.Utc.Id,
                Locale = "en-US"
            };

            var result = _service.CalculateNextExecution(config);

            result.NextExecutionTime.ShouldNotBeNull();
            result.NextExecutionTime.Value.Day.ShouldBe(8);
            result.NextExecutionTime.Value.Hour.ShouldBe(10);
        }

        #endregion Weekly Pattern (DaysOfWeek)

        #region Week Recurrence (RecursEvery)
        #endregion Week Recurrence (RecursEvery)

        #region Anchor & Default Behavior

        [Fact]
        public void CalculateNextExecution_WhenNoDailyFrequency_UsesAnchorTimeAndIgnoresExecutionDateTimeLocal()
        {
            // Arrange
            SchedulerConfiguration config = new()
            {
                CurrentDate = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), // Monday 04 at 10:00 AM.
                Enabled = true,
                Type = SchedulerType.Recurring,
                Occurs = OccursType.Weekly,
                ExecutionDateTimeLocal = new DateTimeOffset(2026, 5, 20, 11, 0, 0, TimeSpan.Zero).AddHours(5), // 03:00 PM  Should be ignored
                RecursEvery = 1,
                WeeklyConfiguration = new() { DaysOfWeek = [DayOfWeek.Monday] },
                TimeZoneId = TimeZoneInfo.Utc.Id,
                Locale = "en-US"

            };

            var result = _service.CalculateNextExecution(config);

            // Assert: Next Monday (11) at 10:00 AM
            result.IsSuccess.ShouldBeTrue();
            result.NextExecutionTime.ShouldNotBeNull();
            result.NextExecutionTime.Value.Day.ShouldBe(11);
            result.NextExecutionTime.Value.Hour.ShouldBe(10);
            result.Description.ShouldContain("10:00");
        }

        #endregion Anchor & Default Behavior

        #region Limits Handling
        #endregion Limits Handling

        #region Edge Cases & Boundaries
        #endregion Edge Cases & Boundaries

        #region Time Units Handling
        #endregion Time Units Handling

        #region Description & Localization
        #endregion Description & Localization

        #region Logical Consistency
        #endregion Logical Consistency

    }
}