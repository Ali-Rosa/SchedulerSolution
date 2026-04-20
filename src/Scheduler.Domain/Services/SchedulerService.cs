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
            return CalculateRecurring(currentDate, config);
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

    private SchedulerResponse CalculateRecurring(DateTime currentDate, ScheduleConfiguration config)
    {
        if (config.Every <= 0)
            return new SchedulerResponse("The 'Every' value must be greater than 0.");

        var candidate = currentDate;

        if (config.ExecutionDateTime.HasValue)
            candidate = (DateTime)config.ExecutionDateTime;

        var startDateOnly = (DateTime)config.StartDate;

        var endDateOnly = config.EndDate ?? DateTime.MaxValue;

        candidate = candidate.AddDays(config.Every);

        // We obtain all valid dates
        var executions = GetAllExecutionsInRange(candidate, startDateOnly, endDateOnly, config);

        if (executions.Count == 0)
            return new SchedulerResponse("There are no executions within the allowed range.");

        // The candidate is the first date on the list
        var nextExecution = executions[0];

        var description = $"Occurs every {config.Every} day(s). Schedule will be used on {candidate:dd/MM/yyyy} at {candidate:HH:mm} starting on {config.StartDate:dd/MM/yyyy}";

        return new SchedulerResponse(candidate, description);
    }

    public List<DateTime> GetAllExecutionsInRange(DateTime Candidate, DateTime from, DateTime to, ScheduleConfiguration config)
    {
        var executionDates = new List<DateTime>();

        if (config.Type == ScheduleType.Recurring)
        {
            if (config.Every <= 0)
                return executionDates;
            
            var candidate = Candidate;

            int Desiredquantity = 1; // The occurrence is the one that will control this in the future
            int attempts = 0;

            while (attempts < Desiredquantity && candidate <= to)
            {
                if (candidate >= from)
                {
                    executionDates.Add(candidate);
                }
                attempts++;
                candidate = candidate.AddDays(config.Every);
            }
        }

        return executionDates;
    }
}