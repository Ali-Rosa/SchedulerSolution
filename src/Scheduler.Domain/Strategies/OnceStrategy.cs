using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

/// <summary>
/// Estrategia para Once
/// </summary>
public class OnceStrategy : IRecurrenceStrategy
{
    public DateTime? CalculateNext(DateTime currentDate, ScheduleConfiguration config)
    {
        if (!config.Enabled)
            return null;

        var candidate = config.ExecutionDateTime;

        // la Fecha de ejecucuin no puede ser menor al currentdate(Fecha Actual)
        if (candidate < currentDate)
            return null;

        // la fecha de ejecucion no puede ser menor a la fecha de inicio
        if (candidate < config.StartDate)
            return null;

        // la fecha de ejecucion no puede ser mayor a la fecha de fin (si se especifica)
        if (config.EndDate.HasValue && candidate > config.EndDate.Value)
            return null;

        return candidate;
    }

    public string GenerateDescription(ScheduleConfiguration config)
    {
        var executionDate = config.ExecutionDateTime;
        var startDate = config.StartDate;
        var endDateText = config.EndDate.HasValue 
            ? $" and ending on {config.EndDate:dd/MM/yyyy}" 
            : string.Empty;

        return $"Occurs once on {executionDate:dd/MM/yyyy} at {executionDate:HH:mm} " +
               $"starting on {startDate:dd/MM/yyyy}{endDateText}.";
    }

    public bool IsValidConfiguration(ScheduleConfiguration config)
    {
        return config.Type == ScheduleType.Once;
    }
}