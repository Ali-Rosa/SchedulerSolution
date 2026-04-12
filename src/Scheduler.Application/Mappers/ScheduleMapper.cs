using Scheduler.Application.DTOs;
using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Models;

namespace Scheduler.Application.Mappers;

/// <summary>
/// Mapper del DTO de Request a dominio del programador
/// </summary>
public static class ScheduleMapper
{
    public static ScheduleConfiguration ToDomain(ScheduleRequestDto dto)
    {
        if (!Enum.TryParse<ScheduleType>(dto.Type, out var scheduleType))
            throw new InvalidScheduleConfigurationException($"Tipo de schedule inválido: {dto.Type}");

        if (!Enum.TryParse<OccursType>(dto.Occurs, out var occursType))
            throw new InvalidScheduleConfigurationException($"Tipo de ocurrencia inválido: {dto.Occurs}");

        return new ScheduleConfiguration(
            Type: scheduleType,
            ExecutionDateTime: dto.ExecutionDateTime,
            Occurs: occursType,
            Every: dto.Every,
            StartDate: dto.StartDate,
            EndDate: dto.EndDate,
            Enabled: dto.Enabled
        );
    }
}
