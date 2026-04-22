using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    private readonly Dictionary<ScheduleType, IScheduleStrategy> _strategies;

    public SchedulerService(IEnumerable<IScheduleStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Type);
    }

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config)
    {
        if (config is null)
            return new SchedulerResponse("The configuration cannot be null.");

        if (!config.Enabled)
            return new SchedulerResponse("The schedule is disabled.");

        if (!Enum.IsDefined(config.Type))
            return new SchedulerResponse("Not defined schedule type.");

        if (!_strategies.TryGetValue(config.Type, out var strategy))
            return new SchedulerResponse("Unsupported schedule type.");

        if (!TryGetTimeZone(config.TimeZoneId, out var timeZone))
            return new SchedulerResponse($"Invalid TimeZoneId: {config.TimeZoneId}");

        var currentLocalTime = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        return strategy.CalculateNextExecution(currentDateUtc, currentLocalTime, config);
    }

    private static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo
            .GetSystemTimeZones()
            .FirstOrDefault(tz => tz.Id == timeZoneId)!;

        return timeZone != null;
    }

}