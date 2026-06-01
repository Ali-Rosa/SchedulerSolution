namespace Scheduler.Domain.Models.Daily;

public static class ScheduleDailyFrequencyExtensions
{
    public static (bool IsValid, string Error) Validate(this ScheduleDailyFrequency config)
    {
        if (config.OccursEveryEnable)
        {
            if (!Enum.IsDefined(config.IntervalUnit))
                return (false, "Not defined interval unit for daily frequency.");
    
            if (config.FrequencyInterval <= 0)
                return (false, "The frequency interval must be greater than 0.");
        }
    
        return (true, string.Empty);
    }
}
