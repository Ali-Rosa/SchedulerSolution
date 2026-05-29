namespace Scheduler.Domain.Models.Monthly;

public sealed record SchedulerMonthly
{ 
    public bool IsSpecificDay { get; init; }
    public int? SpecificDayNumber { get; init; }
    public MonthlyRelativeOrdinal? RelativeOrdinal { get; init; }
    public MonthlyRelativeDayType? RelativeDayType { get; init; }
}