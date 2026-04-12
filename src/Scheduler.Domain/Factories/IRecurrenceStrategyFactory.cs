using Scheduler.Domain.Models;
using Scheduler.Domain.Strategies;

namespace Scheduler.Domain.Factories;

/// <summary>
/// fabrica
/// </summary>
public interface IRecurrenceStrategyFactory
{
    IRecurrenceStrategy Create(ScheduleType type, OccursType occursType);
}