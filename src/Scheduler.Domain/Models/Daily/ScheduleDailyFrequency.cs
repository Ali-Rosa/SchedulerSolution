namespace Scheduler.Domain.Models.Daily;

public sealed record ScheduleDailyFrequency (
    TimeOnly OnceTime,
    bool OccursEveryEnable,
    TimeIntervalUnit IntervalUnit,
    int FrequencyInterval,
    TimeOnly StartTime,
    TimeOnly EndTime
)
{
    public (bool IsValid, string Error) Validate()
    {
        if (OccursEveryEnable)
        {
            if (!Enum.IsDefined(IntervalUnit)) return (false, "Not defined interval unit for daily frequency.");

            if (FrequencyInterval <= 0) return (false, "The frequency interval must be greater than 0.");
        }

        return (true, string.Empty);
    }
};