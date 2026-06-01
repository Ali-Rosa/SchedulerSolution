using Scheduler.Domain.Localization;
using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Validators;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    private readonly Dictionary<StrategyKey, ISchedulerStrategy> _strategies;

    public SchedulerService(IEnumerable<ISchedulerStrategy> strategies) => _strategies = strategies.ToDictionary(s => s.Key);

    public SchedulerResponse CalculateNextExecution(SchedulerConfiguration config)
    {
        if (config is null)
        {
            var defaultLocalizer = SchedulerLocalizerFactory.GetLocalizer(null);
            return new SchedulerResponse(defaultLocalizer.GetValidationError(ValidationErrorKey.ConfigNull));
        }

        var (isValidConfig, configError) = config.Validate();
        if (!isValidConfig) return new SchedulerResponse(configError);

        var (isEnvValid, envError, timeZone) = SchedulerEnvironmentValidator.Validate(config);
        if (!isEnvValid) return new SchedulerResponse(envError);

        var key = new StrategyKey(config.Type, config.Occurs);
        if (!_strategies.TryGetValue(key, out var strategy))
        {
            var localizer = SchedulerLocalizerFactory.GetLocalizer(config.Locale);
            return new SchedulerResponse(localizer.GetValidationError(ValidationErrorKey.UnsupportedCombination));
        }

        return strategy.CalculateNextExecution(config, timeZone!);
    }
}