using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Factories;

/// <summary>
/// Implementacion actual 
/// lo dejo ya la lista para la inyeccion de BD para un futuro si se amerita
/// </summary>
public class RecurrenceStrategyFactory : IRecurrenceStrategyFactory
{
    private readonly Dictionary<(ScheduleType, OccursType), IRecurrenceStrategy> _strategies;

    public RecurrenceStrategyFactory()
    {
        _strategies = new Dictionary<(ScheduleType, OccursType), IRecurrenceStrategy>
        {
            { (ScheduleType.Once, OccursType.Daily), new OnceStrategy() },
            { (ScheduleType.Recurring, OccursType.Daily), new RecurrenceStrategy() }
        };
    }

    public IRecurrenceStrategy Create(ScheduleType type, OccursType occursType)
    {
        var key = (type, occursType);

        if (_strategies.TryGetValue(key, out var strategy))
            return strategy;

        throw new InvalidScheduleConfigurationException(
            $"No existe estrategia para Type={type} y Occurs={occursType}. " +
            "Esto se puede resolver añadiendo una nueva estrategia (OCP).");
    }
}