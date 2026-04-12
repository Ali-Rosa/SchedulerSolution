using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Factories;
using Scheduler.Domain.Models;

namespace Scheduler.Domain.Services;

/// <summary>
/// Implementacion concreta del calculador y punto principal de la orquestacion de la estrategia
/// </summary>
public class ScheduleCalculator : IScheduleCalculator
{
    private readonly IRecurrenceStrategyFactory _factory;

    public ScheduleCalculator(IRecurrenceStrategyFactory factory)
    {
        _factory = factory;
    }

    public ExecutionResult CalculateNextExecution(DateTime currentDate, ScheduleConfiguration config)
    {
        if (config == null)
            throw new InvalidScheduleConfigurationException("La configuracion no puede ser nula.");

        var strategy = _factory.Create(config.Type, config.Occurs);

        if (!strategy.IsValidConfiguration(config))
            throw new InvalidScheduleConfigurationException("Configuracion invalida segun el tipo de Programacion.");

        var nextExecution = strategy.CalculateNext(currentDate, config);
        var description = strategy.GenerateDescription(config);

        return new ExecutionResult(nextExecution, description);
    }
}