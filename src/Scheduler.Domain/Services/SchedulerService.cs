using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Validators;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    private readonly Dictionary<SchedulerStrategyKey, ISchedulerStrategy> _strategies;
    public SchedulerService(IEnumerable<ISchedulerStrategy> strategies) => _strategies = strategies.ToDictionary(s => s.Key);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, SchedulerConfiguration config)
    {
        var (isValidConfig, configError) = ScheduleConfigurationValidator.Validate(config);
        if (!isValidConfig)
            return new SchedulerResponse(configError);

        var key = new SchedulerStrategyKey(config.Type, config.Occurs);
        if (!_strategies.TryGetValue(key, out var strategy))
            return new SchedulerResponse("Unsupported schedule and occurs combination.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);

        return strategy.CalculateNextExecution(currentDateUtc, config, timeZone);

    }

}