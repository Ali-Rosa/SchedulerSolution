using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

/// <summary>
/// Estrategia para Recurring
/// </summary>
public class RecurrenceStrategy : IRecurrenceStrategy
{
    public DateTime? CalculateNext(DateTime currentDate, ScheduleConfiguration config)
    {
        if (!config.Enabled || config.Every <= 0)
            return null;

        var candidate = config.ExecutionDateTime;

        if (candidate >= config.StartDate)
        {
            // la fecha de ejecucion no puede ser mayor a la fecha de fin si se seteo
            if (!config.EndDate.HasValue || candidate <= config.EndDate.Value)
            {
                var result = candidate.AddDays(config.Every);

                // verifico nuevamente que este dentro del rango
                if (config.EndDate.HasValue && result > config.EndDate.Value)
                    return null;

                return result;
            }
            else
            {
                // si la fecha es mayor a la fecha final
                return null;
            }
        } 
        else 
        { 
            return null; 
        }
    }

    public string GenerateDescription(ScheduleConfiguration config)
    {
        var dayText = config.Every == 1 ? "day" : $"{config.Every} days";
        var executionDate = config.ExecutionDateTime;
        var startDate = config.StartDate;
        var endDateText = config.EndDate.HasValue 
            ? $" and ending on {config.EndDate:dd/MM/yyyy}" 
            : string.Empty;

        return $"Occurs every {dayText} at {executionDate:HH:mm} " +
               $"starting on {startDate:dd/MM/yyyy}{endDateText}.";
    }

    public bool IsValidConfiguration(ScheduleConfiguration config)
    {
        return config.Type == ScheduleType.Recurring && config.Every > 0;
    }
}