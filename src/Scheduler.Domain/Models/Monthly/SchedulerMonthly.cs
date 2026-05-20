namespace Scheduler.Domain.Models.Monthly;

public sealed record SchedulerMonthly(
    bool IsSpecificDay,
    int? SpecificDayNumber,
    MonthlyRelativeOrdinal? RelativeOrdinal,
    MonthlyRelativeDayType? RelativeDayType
)
{
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
};