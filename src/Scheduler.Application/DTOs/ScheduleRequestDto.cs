namespace Scheduler.Application.DTOs;

/// <summary>
/// DTO de Aplicacion
/// </summary>
public record ScheduleRequestDto(
    string Type,
    DateTime ExecutionDateTime,
    string Occurs,
    int Every,
    DateTime StartDate,
    DateTime? EndDate,
    bool Enabled
);
