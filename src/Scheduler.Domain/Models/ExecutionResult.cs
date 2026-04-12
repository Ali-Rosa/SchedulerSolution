namespace Scheduler.Domain.Models;

/// <summary>
/// Resultado de la ejecucion 
/// Una Fecha  de la Proxima ejecucion y un mensaje, que detalla datos relevantes, por ahora
/// </summary>
public record ExecutionResult(
    DateTime? NextExecutionTime,
    string Description
);