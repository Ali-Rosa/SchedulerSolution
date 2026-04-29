using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class WeeklyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.Weekly is null)
            return new SchedulerResponse("Weekly configuration is required.");

        if (config.Every <= 0)
            return new SchedulerResponse("The Every value must be greater than 0.");

        // Convert current UTC to local
        var currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);

        // Determine start date reference for weekly calculation
        var startLocal = TimeZoneInfo.ConvertTime(
            config.StartDateLocal ?? currentLocal,
            timeZone
        );

        var startDay = DateOnly.FromDateTime(startLocal.DateTime);
        var cursorDay = DateOnly.FromDateTime(currentLocal.DateTime);

        // Safety guard: search forward max 1 year
        for (int i = 0; i < 366; i++)
        {
            // Check weekly rules (day of week + every N weeks)
            if (!WeeklyCalendarRule.IsValidDay(cursorDay, startDay, config.Weekly))
            {
                cursorDay = cursorDay.AddDays(1);
                continue;
            }

            IEnumerable<DateTimeOffset> executions;

            // IntraDay frequency
            if (config.IntraDay is not null)
            {
                executions = IntraDayRule
                    .GetExecutionsForDay(cursorDay, config.IntraDay, timeZone);
            }
            else
            {
                // Default: once at start of day

                var localDateTime = cursorDay.ToDateTime(new TimeOnly(0, 0));

                var localOffset = new DateTimeOffset(
                    localDateTime,
                    timeZone.GetUtcOffset(localDateTime)
                );

                executions = new[]
                {
                    localOffset.ToUniversalTime()
                };
            }

            // Take first execution after current time
            var nextExecutionUtc = executions
                .Where(e => e > currentUtc)
                .OrderBy(e => e)
                .FirstOrDefault();

            if (nextExecutionUtc != default)
            {
                var nextLocal = TimeZoneInfo.ConvertTime(nextExecutionUtc, timeZone);

                var description =
                    $"Occurs weekly. Schedule will be used on {nextLocal:dd/MM/yyyy} " +
                    $"at {nextLocal:HH:mm}";

                return new SchedulerResponse(nextLocal, description);
            }

            cursorDay = cursorDay.AddDays(1);
        }

        return new SchedulerResponse("No valid weekly execution found.");
    }
}