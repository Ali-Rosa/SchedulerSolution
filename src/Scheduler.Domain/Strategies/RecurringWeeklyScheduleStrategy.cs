using Scheduler.Domain.Models;
using Scheduler.Domain.Rules;

namespace Scheduler.Domain.Strategies;

public sealed class RecurringWeeklyScheduleStrategy : IScheduleStrategy
{
    public ScheduleStrategyKey Key => new(ScheduleType.Recurring, OccursType.Weekly);

    public SchedulerResponse CalculateNextExecution(DateTimeOffset currentDateUtc, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.Weekly is null)
            return new SchedulerResponse("Weekly configuration is required.");

        var currentLocal = TimeZoneInfo.ConvertTime(currentDateUtc, timeZone);

        var seriesStartLocal = TimeZoneInfo.ConvertTime(config.LimitsStartDateLocal ?? config.ExecutionDateTimeLocal ?? currentDateUtc, timeZone);

        var searchCursorLocal = currentLocal > seriesStartLocal ? currentLocal : seriesStartLocal;

        for (int i = 0; i < 366; i++)
        {
            var currentDay = DateOnly.FromDateTime(searchCursorLocal.DateTime);
            var seriesStartDay = DateOnly.FromDateTime(seriesStartLocal.DateTime);

            // Capa 1: ¿Es un día válido según la regla semanal? (Cada N semanas + Días de la semana)
            if (WeeklyCalendarRule.IsValidDay(currentDay, seriesStartDay, config.Weekly))
            {
                // Capa 2: Obtener las ejecuciones intra-día (Daily Frequency)
                var dayExecutions = GetExecutionsForDay(currentDay, config, timeZone);

                // Filtrar la primera que sea futura
                var nextExecution = dayExecutions
                    .Where(e => e > currentDateUtc)
                    .Where(e => !config.LimitsEndDateLocal.HasValue || e <= config.LimitsEndDateLocal.Value)
                    .OrderBy(e => e)
                    .FirstOrDefault();

                if (nextExecution != default)
                {
                    return new SchedulerResponse(nextExecution, BuildDescription(nextExecution, config, timeZone));
                }
            }

            if (config.LimitsEndDateLocal.HasValue && searchCursorLocal > config.LimitsEndDateLocal.Value)
                break;

            searchCursorLocal = new DateTimeOffset(currentDay.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone.GetUtcOffset(searchCursorLocal.DateTime));
        }

        return new SchedulerResponse("No valid weekly execution found.");
    }

    private IEnumerable<DateTimeOffset> GetExecutionsForDay(DateOnly day, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        if (config.DailyFrecuency is not null)
        {
            return DailyFrecuencyRule.GetExecutionsForDay(day, config.DailyFrecuency, timeZone);
        }

        var time = config.ExecutionDateTimeLocal?.TimeOfDay ?? TimeSpan.Zero;
        var localDateTime = day.ToDateTime(TimeOnly.FromTimeSpan(time));
        return new[] { new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime)) };
    }

    private string BuildDescription(DateTimeOffset nextExecution, ScheduleConfiguration config, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(nextExecution, timeZone);
        var days = string.Join(", ", config.Weekly!.DaysOfWeek).ToLower();

        var desc = $"Occurs every {config.Weekly.EveryWeeks} week(s) on {days}. ";

        if (config.DailyFrecuency != null)
            desc += $"Every {config.DailyFrecuency.FrequencyInterval} {config.DailyFrecuency.IntervalUnit.ToString().ToLower()} ";

        desc += $"at {local:HH:mm}. Next: {local:dd/MM/yyyy}";

        return desc;
    }
}