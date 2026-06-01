using Scheduler.Domain.Models;
using Scheduler.Domain.Localization;

namespace Scheduler.Domain.Strategies;

public sealed class OnceDailySchedulerStrategy : ISchedulerStrategy
{
    public StrategyKey Key => new(SchedulerType.Once, OccursType.Daily);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config, TimeZoneInfo timeZone)
    {
        var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);
        var candidate = config.CurrentDate;

        if (config.ExecutionDateTimeLocal.HasValue)
        {
            if (candidate > config.ExecutionDateTimeLocal.Value)
                return new SchedulerResponse(localizer.GetValidationError(ValidationErrorKey.ExecutionInPast));

            if (candidate < config.ExecutionDateTimeLocal.Value)
                candidate = config.ExecutionDateTimeLocal.Value;
        }

        if (config.LimitsStartDateLocal.HasValue && candidate < config.LimitsStartDateLocal.Value)
            return new SchedulerResponse(localizer.GetValidationError(ValidationErrorKey.ExecutionBeforeLimits));

        if (config.LimitsEndDateLocal.HasValue && candidate > config.LimitsEndDateLocal.Value)
            return new SchedulerResponse(localizer.GetValidationError(ValidationErrorKey.ExecutionAfterLimits));

        DateTimeOffset candidateLocalTime = TimeZoneInfo.ConvertTime(candidate, timeZone);

        var description = localizer.BuildOnceDescription(candidateLocalTime, config.LimitsStartDateLocal, timeZone);

        return new SchedulerResponse(candidateLocalTime, description);
    }
}
