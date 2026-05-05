using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

public sealed class OnceDailyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Once, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        var candidate = currentDateUtc;

        if (config.ExecutionDateTimeLocal.HasValue)
        {
            if (candidate > config.ExecutionDateTimeLocal.Value)
                return new SchedulerResponse("DateTime cannot be less than CurrentDate");

            if (candidate < config.ExecutionDateTimeLocal.Value)
                candidate = config.ExecutionDateTimeLocal.Value;
        }

        if ((config.LimitsStartDateLocal.HasValue && candidate < config.LimitsStartDateLocal.Value)
            || (config.LimitsEndDateLocal.HasValue && candidate > config.LimitsEndDateLocal.Value))
            return new SchedulerResponse("The execution date is outside the allowed range.");

        // OUTPUTS
        DateTimeOffset candidateLocalTime = TimeZoneInfo.ConvertTime(candidate, timeZone!);
        
        var description = $"Occurs once. Schedule will be used on {candidateLocalTime:dd/MM/yyyy} "
            + $"at {candidateLocalTime:HH:mm} ";

        if (config.LimitsStartDateLocal.HasValue)
        {
            DateTimeOffset StartDateCandidatoLocalTime = TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal!.Value, timeZone!);
            description += $"starting on {StartDateCandidatoLocalTime:dd/MM/yyyy}";
        }      

        return new SchedulerResponse(candidateLocalTime, description);
    }

}
