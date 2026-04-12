using Scheduler.Domain.Models;

namespace Scheduler.Domain.Strategies;

/// <summary>
/// Cada tipo de recurrencia jala su propia logica
/// estrategia orientda a los tipos de ocurrencias del programador
/// </summary>
public interface IRecurrenceStrategy
{
    DateTime? CalculateNext(DateTime currentDate, ScheduleConfiguration config);
    string GenerateDescription(ScheduleConfiguration config);
    bool IsValidConfiguration(ScheduleConfiguration config);
}