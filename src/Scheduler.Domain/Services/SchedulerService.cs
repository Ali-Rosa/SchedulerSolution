using Scheduler.Domain.Models;
using System.Globalization;

namespace Scheduler.Domain.Services;

public class SchedulerService
{
    public SchedulerResponse CalculateNextExecution(DateTime currentDate, ScheduleConfiguration config)
    {
        if (!config.Enabled)
            return new SchedulerResponse("The schedule is disabled.");

        if (config.EndDate.HasValue && config.StartDate > config.EndDate.Value)
            return new SchedulerResponse("StartDate cannot be greater than EndDate.");

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

        var candidate = config.ExecutionDateTime ?? currentDate;

        var executions = GetAllExecutionsInRange(candidate, config);

        if (executions.Count == 0)
            return new SchedulerResponse("There are no executions within the allowed range.");

        var nextExecution = executions[0];

        var description = $"Occurs every {config.Every} day(s). Schedule will be used on {candidate:dd/MM/yyyy} at {candidate:HH:mm} starting on {config.StartDate:dd/MM/yyyy}";

        return new SchedulerResponse(nextExecution, description);
    }

    public List<DateTime> GetAllExecutionsInRange(DateTime start, ScheduleConfiguration config)
    {
        var executionDates = new List<DateTime>();

        if (config.Type != ScheduleType.Recurring)
            return executionDates;

        if (config.Every <= 0)
            return executionDates;

        var candidate = start.AddDays(config.Every);

        var (windowStart, windowEnd) = GetActiveWindow(candidate, config);

        if (config.EndDate.HasValue && windowEnd > config.EndDate.Value)
            windowEnd = config.EndDate.Value;

        while (candidate <= windowEnd)
        {
            if (candidate >= config.StartDate && (!config.EndDate.HasValue || candidate <= config.EndDate.Value))
            {
                executionDates.Add(candidate);
            }

            candidate = candidate.AddDays(config.Every);
        }

        return executionDates;

    }

    private (DateTime windowStart, DateTime windowEnd) GetActiveWindow(DateTime candidate, ScheduleConfiguration config)
    {
        return config.Occurs switch
        {
            OccursType.Daily =>
                (candidate.Date, candidate.Date.AddDays(1).AddTicks(-1)),

            OccursType.Weekly =>
                GetRankWeekly(candidate),

            OccursType.Monthly =>
                GetRankMonthly(candidate),

            _ =>
                (candidate.Date, candidate.Date.AddDays(1).AddTicks(-1))
        };
    }

    public (DateTime firstDay, DateTime lastDay) GetRankWeekly(DateTime candidate, DayOfWeek FirstDayWeek = DayOfWeek.Monday)
    {
        int difference = (7 + (candidate.DayOfWeek - FirstDayWeek)) % 7;

        DateTime firstDay = candidate.AddDays(-difference).Date;
        DateTime lastDay = (firstDay.AddDays(6)).AddDays(1).AddTicks(-1);

        return (firstDay, lastDay);
    }

    public (DateTime firstDay, DateTime lastDay) GetRankMonthly(DateTime candidate)
    {
        DateTime firstDay = new DateTime(candidate.Year, candidate.Month, 1);

        DateTime lastDay = (firstDay.AddMonths(1).AddDays(-1)).AddDays(1).AddTicks(-1);

        return (firstDay, lastDay);
    }





}