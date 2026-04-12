using Scheduler.Application.DTOs;
using Scheduler.Application.Mappers;
using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Models;
using Scheduler.Domain.Services;

namespace Scheduler.Application.UseCases;

/// <summary>
/// Recibo los datos y delego la mision al dominio ahora
/// </summary>
public class CalculateNextExecutionUseCase
{
    private readonly IScheduleCalculator _calculator;

    public CalculateNextExecutionUseCase(IScheduleCalculator calculator)
    {
        _calculator = calculator;
    }

    public ExecutionResult Execute(DateTime currentDate, ScheduleRequestDto dto)
    {
        try
        {
            ValidateInput(currentDate, dto);
            var config = ScheduleMapper.ToDomain(dto);
            return _calculator.CalculateNextExecution(currentDate, config);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidScheduleConfigurationException($"Validación de entrada fallida: {ex.Message}", ex);
        }
    }

    private void ValidateInput(DateTime currentDate, ScheduleRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (currentDate == default)
            throw new ArgumentException("CurrentDate no puede ser null", nameof(currentDate));

        if (string.IsNullOrWhiteSpace(dto.Type))
            throw new ArgumentException("Type no puede estar vacío", nameof(dto.Type));

        if (string.IsNullOrWhiteSpace(dto.Occurs))
            throw new ArgumentException("Occurs no puede estar vacío", nameof(dto.Occurs));
    }
}
