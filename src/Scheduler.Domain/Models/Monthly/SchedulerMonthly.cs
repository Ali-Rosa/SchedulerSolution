namespace Scheduler.Domain.Models.Monthly;

public sealed record SchedulerMonthly
{
    public bool IsSpecificDay { get; init; }
    public int? SpecificDayNumber { get; init; }
    public MonthlyRelativeOrdinal? RelativeOrdinal { get; init; }
    public MonthlyRelativeDayType? RelativeDayType { get; init; }

    public (bool IsValid, string Error) Validate()
    {
        if (IsSpecificDay)
        {
            if (!SpecificDayNumber.HasValue || SpecificDayNumber < 1 || SpecificDayNumber > 31)
                return (false, "The day must be between 1 and 31.");
        }
        else
        {
            if (!RelativeOrdinal.HasValue || !Enum.IsDefined(RelativeOrdinal.Value))
                return (false, $"Not defined relative ordinal: {RelativeOrdinal}.");

            if (!RelativeDayType.HasValue || !Enum.IsDefined(RelativeDayType.Value))
                return (false, $"Not defined relative day type: {RelativeDayType}.");
        }

        return (true, string.Empty);
    }
}