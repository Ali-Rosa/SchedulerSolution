using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;
using Scheduler.Domain.Validators;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    private readonly Dictionary<ScheduleStrategyKey, IScheduleStrategy> _strategies;

    public SchedulerService(IEnumerable<IScheduleStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Key);
    }

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config)
    {
        var (isValid, errorMessage) = ScheduleConfigurationValidator.Validate(config);
        if (!isValid)
            return new SchedulerResponse(errorMessage);

        var key = new ScheduleStrategyKey(config.Type, config.Occurs);
        if (!_strategies.TryGetValue(key, out var strategy))
            return new SchedulerResponse("Unsupported schedule and occurs combination.");

        
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);
        return strategy.CalculateNextExecution(currentDateUtc, config, timeZone);

    }

}