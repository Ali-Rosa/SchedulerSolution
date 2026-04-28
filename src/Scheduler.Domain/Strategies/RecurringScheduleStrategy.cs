using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

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

            ////////////////////////////////////////////////////////////////////////
            var candidateLocal = TimeZoneInfo.ConvertTime(candidate, timeZone);
            
            DateOnly day = DateOnly.FromDateTime(candidateLocal.DateTime);

            ///// FOR WEEKLY

            if (config.Weekly is not null)
            {
                var startLocal = TimeZoneInfo.ConvertTime(config.StartDateLocal ?? candidate, timeZone);

                var startDay = DateOnly.FromDateTime(startLocal.DateTime);

                if (!WeeklyCalendarRule.IsValidDay(day, startDay, config.Weekly))
                {
                    return new SchedulerResponse("The selected day is not valid for the weekly schedule.");
                }
            }

            ///// FOR iNTRAdAY
            IEnumerable<DateTimeOffset> executions;

            if (config.IntraDay is not null)
            {
                executions = IntraDayRule.GetExecutionsForDay(day, config.IntraDay, timeZone);
            }
            else
            {
                executions = new[]
                {
                    candidateLocal.ToUniversalTime()
                };
            }

            var nextExecution = executions .Where(e => e > currentDateUtc) .OrderBy(e => e) .FirstOrDefault();

            if (nextExecution == default)
                return new SchedulerResponse("No valid executions found.");


            
            
            ////////////  OUTPUTS  ///////////
            DateTimeOffset nextExecutionLocal = TimeZoneInfo.ConvertTime(nextExecution, timeZone!);

            var description = $"Occurs every day. Schedule will be used on {nextExecutionLocal:dd/MM/yyyy} "
                + $"at {nextExecutionLocal:HH:mm} ";

            if (config.StartDateLocal.HasValue)
            {
                DateTimeOffset StartDateCandidatoLocalTime = TimeZoneInfo.ConvertTime(config.StartDateLocal!.Value, timeZone!);
                description += $"starting on {StartDateCandidatoLocalTime:dd/MM/yyyy}";
            }

            return new SchedulerResponse(nextExecutionLocal, description);
        }


    }

}