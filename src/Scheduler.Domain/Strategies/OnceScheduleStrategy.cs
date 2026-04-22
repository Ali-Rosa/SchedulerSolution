using Scheduler.Domain.Models;


namespace Scheduler.Domain.Strategies
{
    public sealed class OnceScheduleStrategy : IScheduleStrategy
    {
        public ScheduleType Type => ScheduleType.Once;

        public SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, DateTimeOffset _ /* currentLocalTime */, ScheduleConfiguration config)
        {
            if (!config.ExecutionDateTimeUtc.HasValue)
                return new SchedulerResponse("ExecutionDateTimeUtc is required for a one-time schedule.");

            var candidate = config.ExecutionDateTimeUtc.Value;

            if (candidate < currentUtc)
                return new SchedulerResponse("DateTime greater than CurrentDate");

            //if (config.StartDateUtc.HasValue && candidate < config.StartDateUtc.Value)
            //    return new SchedulerResponse("The execution date is prior to the start date.");

            if (config.EndDateUtc.HasValue && candidate > config.EndDateUtc.Value)
                return new SchedulerResponse("The execution date is outside the allowed range.");

            var description =
                $"Occurs once. Schedule will be used on {candidate:dd/MM/yyyy} " +
                $"at {candidate:HH:mm} UTC";

            return new SchedulerResponse(candidate, description);
        }
    }
}
