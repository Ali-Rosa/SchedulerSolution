using Scheduler.Domain.Models;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    public SchedulerResponse CalculateNextExecution(DateTime currentDate, ScheduleConfiguration config)
    {
        if (!config.Enabled)
            return new SchedulerResponse("The schedule is disabled.");

        if (config.Type == ScheduleType.Once)
        {
            return CalculateOnce(currentDate, config);
        }
        else if (config.Type == ScheduleType.Recurring)
        {
            return CalculateDaily(currentDate, config);
        }

        return new SchedulerResponse("Unsupported schedule type.");
    }

    private SchedulerResponse CalculateOnce(DateTime currentDate, ScheduleConfiguration config)
    {
        var candidate = config.ExecutionDateTime;

        if (candidate < currentDate)
            return new SchedulerResponse("The one-time execution date has already passed.");

        if (candidate < config.StartDate)
            return new SchedulerResponse("The execution date is prior to the start date.");

        if (config.EndDate.HasValue && candidate > config.EndDate.Value)
            return new SchedulerResponse("The execution date is outside the allowed range.");

        var description = $"Occurs once. Schedule will be used on {candidate:dd/MM/yyyy} at {candidate:HH:mm} starting on {config.StartDate:dd/MM/yyyy}";

        return new SchedulerResponse(candidate, description);
    }

    private SchedulerResponse CalculateDaily(DateTime currentDate, ScheduleConfiguration config)
    {
        if (config.Every <= 0)
            return new SchedulerResponse("The 'Every' value must be greater than 0.");

        var startDateOnly = config.StartDate.Date;
        var currentDateOnly = currentDate.Date;
        var timeOfDay = config.ExecutionDateTime.TimeOfDay;

        DateTime nextDate;

        if (startDateOnly > currentDateOnly)
        {
            nextDate = startDateOnly + timeOfDay;
        }
        else
        {
            int daysSinceStart = (currentDateOnly - startDateOnly).Days;
            int periods = (daysSinceStart + config.Every - 1) / config.Every;
            int daysToAdd = periods * config.Every;

            nextDate = startDateOnly.AddDays(daysToAdd) + timeOfDay;

            if (nextDate <= currentDate)
                nextDate = nextDate.AddDays(config.Every);
        }

        if (config.EndDate.HasValue && nextDate > config.EndDate.Value)
            return new SchedulerResponse("There are no more executions within the allowed range.");

        var description = $"Occurs every {config.Every} day(s). Schedule will be used on {nextDate:dd/MM/yyyy} at {nextDate:HH:mm} starting on {config.StartDate:dd/MM/yyyy}";

        return new SchedulerResponse(nextDate, description);
    }
}