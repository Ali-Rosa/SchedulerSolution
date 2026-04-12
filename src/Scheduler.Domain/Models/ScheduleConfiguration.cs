namespace Scheduler.Domain.Models;

/// <summary>
/// Solo guardo configuración del programador(Schedule)
/// Value object
/// </summary>
public record ScheduleConfiguration(
    ScheduleType Type,
    DateTime ExecutionDateTime,
    OccursType Occurs,
    int Every,      // ojo aqui, este lo tomo en cuenta solo en caso de programacion 'Recurring'
    DateTime StartDate,
    DateTime? EndDate,
    bool Enabled // no le encuentro logica funcional en la imagen por ahora (debo Preguntar a Juan)
);