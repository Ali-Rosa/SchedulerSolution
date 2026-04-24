using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies
{
    public sealed class RecurringScheduleStrategy : IScheduleStrategy
    {
        public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Daily);

        public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
        {
            var candidate = currentDateUtc;

            if (config.Every <= 0)
                return new SchedulerResponse("The Every value must be greater than 0.");

            if (config.ExecutionDateTimeLocal.HasValue)
            {
                if (candidate > config.ExecutionDateTimeLocal.Value)
                    return new SchedulerResponse("DateTime cannot be less than CurrentDate");

                if (candidate < config.ExecutionDateTimeLocal.Value)
                    candidate = config.ExecutionDateTimeLocal.Value;
            }

            if (config.StartDateLocal.HasValue && candidate < config.StartDateLocal.Value)
                candidate = config.StartDateLocal.Value;

            candidate = candidate.AddDays(config.Every);

            if (config.EndDateLocal.HasValue && candidate > config.EndDateLocal.Value)
                return new SchedulerResponse("The execution date is outside the allowed range.");

            /// CALCULATE DATE RANGE
            var executions = GetAllExecutionsInRange(candidate, config);

            //  VALIDATE IF THERE ARE EXECUTIONS IN THE RANGE 
            if (executions.Count == 0)
                return new SchedulerResponse("There are no executions within the allowed range.");

            var nextExecution = executions[0]; // ONLY ONE FOR DAILY, SO TAKE THE FIRST ONE FOR NOW

            //  OUTPUTS
            DateTimeOffset candidateLocalTime = TimeZoneInfo.ConvertTime(nextExecution, timeZone!);

            var description = $"Occurs every day. Schedule will be used on {candidateLocalTime:dd/MM/yyyy} "
                + $"at {candidateLocalTime:HH:mm} ";

            if (config.StartDateLocal.HasValue)
            {
                DateTimeOffset StartDateCandidatoLocalTime = TimeZoneInfo.ConvertTime(config.StartDateLocal!.Value, timeZone!);
                description += $"starting on {StartDateCandidatoLocalTime:dd/MM/yyyy}";
            }

            return new SchedulerResponse(candidateLocalTime, description);
        }

        private List<DateTimeOffset> GetAllExecutionsInRange(DateTimeOffset candidate, ScheduleConfiguration config)
        {
            var executionDates = new List<DateTimeOffset>();

            var current = candidate;

            var endDate = config.EndDateLocal ?? DateTimeOffset.Now.AddMonths(1); // safety limit

            while (current <= endDate)
            {
                if ((!config.StartDateLocal.HasValue || current >= config.StartDateLocal.Value) &&
                    (!config.EndDateLocal.HasValue || current <= config.EndDateLocal.Value))
                {
                    executionDates.Add(current);
                }

                current = current.AddDays(config.Every);
            }

            return executionDates;
        }

    }


}


