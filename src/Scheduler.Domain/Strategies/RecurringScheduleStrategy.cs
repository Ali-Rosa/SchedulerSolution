using Scheduler.Domain.Models;


namespace Scheduler.Domain.Strategies
{
    public sealed class RecurringScheduleStrategy : IScheduleStrategy
    {
        public ScheduleType Type => ScheduleType.Recurring;

        public SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, DateTimeOffset _ /* currentLocalTime */, ScheduleConfiguration config)
        {
            if (!config.ExecutionDateTimeUtc.HasValue)
                return new SchedulerResponse("ExecutionDateTimeUtc is required for a one-time schedule.");

            var candidate = config.ExecutionDateTimeUtc.Value;

            if (candidate < currentUtc)
                return new SchedulerResponse("The one-time execution date has already passed.");

            if (config.StartDateUtc.HasValue && candidate < config.StartDateUtc.Value)
                return new SchedulerResponse("The execution date is prior to the start date.");

            if (config.EndDateUtc.HasValue && candidate > config.EndDateUtc.Value)
                return new SchedulerResponse("The execution date is outside the allowed range.");

            var description =
                $"Occurs once. Schedule will be used on {candidate:dd/MM/yyyy} " +
                $"at {candidate:HH:mm} UTC";

            return new SchedulerResponse(candidate, description);
        }






        // public ScheduleType Type => ScheduleType.Recurring;

        //public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDate, DateTimeOffset _ /* currentLocalTime */, ScheduleConfiguration config)
        //{
        //    if (config.Every <= 0)
        //        return new SchedulerResponse("The 'Every' value must be greater than 0.");

        //    var candidate = config.ExecutionDateTimeUtc ?? currentDate;

        //    var executions = GetAllExecutionsInRange(candidate, config);

        //    if (executions.Count == 0)
        //        return new SchedulerResponse("There are no executions within the allowed range.");

        //    var nextExecution = executions[0];

        //    var description =
        //        $"Occurs every {config.Every} day(s). Schedule will be used on {candidate:dd/MM/yyyy} " +
        //        $"at {candidate:HH:mm} starting on {config.StartDateUtc:dd/MM/yyyy}";

        //    return new SchedulerResponse(nextExecution, description);
        //}

        //private List<DateTime> GetAllExecutionsInRange(DateTime start, ScheduleConfiguration config)
        //{
        //    var executionDates = new List<DateTime>();

        //    var candidate = start.AddDays(config.Every);

        //    var (windowStart, windowEnd) = GetActiveWindow(candidate, config);

        //    if (config.EndDateUtc.HasValue && windowEnd > config.EndDateUtc.Value)
        //        windowEnd = config.EndDateUtc.Value;

        //    while (candidate <= windowEnd)
        //    {
        //        if (candidate >= config.StartDateUtc &&
        //            (!config.EndDateUtc.HasValue || candidate <= config.EndDateUtc.Value))
        //        {
        //            executionDates.Add(candidate);
        //        }

        //        candidate = candidate.AddDays(config.Every);
        //    }

        //    return executionDates;
        //}

        //private (DateTime windowStart, DateTime windowEnd)
        //    GetActiveWindow(DateTime candidate, ScheduleConfiguration config)
        //{
        //    return config.Occurs switch
        //    {
        //        OccursType.Daily =>
        //            (candidate.Date, candidate.Date.AddDays(1).AddTicks(-1)),

        //        OccursType.Weekly =>
        //            GetRankWeekly(candidate),

        //        OccursType.Monthly =>
        //            GetRankMonthly(candidate),

        //        _ =>
        //            (candidate.Date, candidate.Date.AddDays(1).AddTicks(-1))
        //    };
        //}

        //private (DateTime firstDay, DateTime lastDay)
        //    GetRankWeekly(DateTime candidate, DayOfWeek firstDay = DayOfWeek.Monday)
        //{
        //    int diff = (7 + (candidate.DayOfWeek - firstDay)) % 7;
        //    var start = candidate.AddDays(-diff).Date;
        //    var end = start.AddDays(7).AddTicks(-1);

        //    return (start, end);
        //}

        //private (DateTime firstDay, DateTime lastDay)
        //    GetRankMonthly(DateTime candidate)
        //{
        //    var start = new DateTime(candidate.Year, candidate.Month, 1);
        //    var end = start.AddMonths(1).AddTicks(-1);

        //    return (start, end);
        //}
    }
}
