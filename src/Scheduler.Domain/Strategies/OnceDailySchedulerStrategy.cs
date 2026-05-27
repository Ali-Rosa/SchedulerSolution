using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

public sealed class OnceDailySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Once, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var candidate = config.CurrentDate;

        if (config.ExecutionDateTimeLocal.HasValue)
        {
            if (candidate > config.ExecutionDateTimeLocal.Value)
                return new SchedulerResponse("DateTime cannot be less than CurrentDate.");

            if (candidate < config.ExecutionDateTimeLocal.Value)
                candidate = config.ExecutionDateTimeLocal.Value;
        }

        if (config.LimitsStartDateLocal.HasValue && candidate < config.LimitsStartDateLocal.Value)
            return new SchedulerResponse("The selected execution date is earlier than the allowed start limit date.");

        if (config.LimitsEndDateLocal.HasValue && candidate > config.LimitsEndDateLocal.Value)
            return new SchedulerResponse("The selected execution date is later than the allowed end limit date.");

        DateTimeOffset candidateLocalTime = TimeZoneInfo.ConvertTime(candidate, timeZone);

        var description = BuildOnceDescription(candidateLocalTime, config.LimitsStartDateLocal, timeZone);     

        return new SchedulerResponse(candidateLocalTime, description);
    }

    private static string BuildOnceDescription(DateTimeOffset candidateLocalTime, DateTimeOffset? limitsStartDateLocal, TimeZoneInfo timeZone)
    {
        var description = $"Occurs once. Schedule will be used on {candidateLocalTime:dd/MM/yyyy} "
            + $"at {candidateLocalTime:HH:mm} ";

        if (limitsStartDateLocal.HasValue)
        {
            DateTimeOffset startDateLocalTime = TimeZoneInfo.ConvertTime(limitsStartDateLocal.Value, timeZone);
            description += $"starting on {startDateLocalTime:dd/MM/yyyy}";
        }

        return description;
    }

}
