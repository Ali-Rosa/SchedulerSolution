using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;

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
        if (config is null)
            return new SchedulerResponse("The configuration cannot be null.");

        if (!config.Enabled)
            return new SchedulerResponse("The schedule is disabled.");

        if (!Enum.IsDefined(config.Type))
            return new SchedulerResponse("Not defined schedule type.");

        if (!Enum.IsDefined(config.Occurs))
            return new SchedulerResponse("Not defined occurs type.");

        if (config.Every < 0)
            return new SchedulerResponse("The Every value cannot be negative.");

        var key = new ScheduleStrategyKey(config.Type, config.Occurs);

        if (!_strategies.TryGetValue(key, out var strategy))
            return new SchedulerResponse("Unsupported schedule and occurs combination.");
        
        if (!TryGetTimeZone(config.TimeZoneId, out var timeZone))
            return new SchedulerResponse($"Invalid TimeZoneId: {config.TimeZoneId}");


        return strategy.CalculateNextExecution(currentDateUtc, config, timeZone!);

    }

    private static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo? timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = null;
            return false;
        }
    }
}