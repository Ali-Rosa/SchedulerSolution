using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Validators;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    private readonly Dictionary<StrategyKey, ISchedulerStrategy> _strategies;

    public SchedulerService(IEnumerable<ISchedulerStrategy> strategies) => _strategies = strategies.ToDictionary(s => s.Key);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config)
    {
        if (config is null) return new SchedulerResponse("The configuration cannot be null.");

        var (isValidConfig, configError) = config.Validate();
        if (!isValidConfig) return new SchedulerResponse(configError);

        var (isEnvValid, envError, timeZone) = SchedulerEnvironmentValidator.Validate(config);
        if (!isEnvValid) return new SchedulerResponse(envError);

        var key = new StrategyKey(config.Type, config.Occurs);
        if (!_strategies.TryGetValue(key, out var strategy))
            return new SchedulerResponse("Unsupported schedule and occurs combination.");

        return strategy.CalculateNextExecution(currentDateUtc, config, timeZone!);

    }
}