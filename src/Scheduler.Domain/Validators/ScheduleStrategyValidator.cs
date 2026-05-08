using Scheduler.Domain.Models;

namespace Scheduler.Domain.Validators
{
    public static class ScheduleStrategyValidator
    {
        public static (bool IsValid, string ErrorMessage) Validate(ScheduleConfiguration config)
        {

            if (config.Type != ScheduleType.Recurring)
                return (true, string.Empty);

            //if (config.Occurs == OccursType.Daily && config.DailyFrecuency is null)
            //        return (false, "Daily Frequency configuration is required for Daily occurs type.");

            if (config.Occurs == OccursType.Weekly && config.Weekly is null)
                    return (false, "Weekly configuration is required for Weekly occurs type.");

            return (true, string.Empty);
        }
    }
}
