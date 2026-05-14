namespace Scheduler.Domain.Models;

public sealed record SchedulerMonthly(
    bool IsSpecificDay,     // True = "Day 8", False = "The First Thursday"
    int? SpecificDayNumber, // 1 - 31
    SchedulerMonthlyRelativeOrdinal? RelativeOrdinal,
    SchedulerMonthlyRelativeDayType? RelativeDayType
);