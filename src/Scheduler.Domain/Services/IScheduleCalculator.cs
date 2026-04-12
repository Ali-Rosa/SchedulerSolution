using Scheduler.Domain.Models;

namespace Scheduler.Domain.Services;

/// <summary>
/// Punto de entrada principal del Dominio
/// </summary>
public interface IScheduleCalculator
{
    ExecutionResult CalculateNextExecution(DateTime currentDate, ScheduleConfiguration config);
}