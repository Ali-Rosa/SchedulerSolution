namespace Scheduler.Domain.Models;

/// <summary>
/// Tipos de programacion para el Programador(Schedule)
/// Unica y Recurrrente por ahora ;)
/// </summary>
public enum ScheduleType
{
    Once,
    Recurring 
}