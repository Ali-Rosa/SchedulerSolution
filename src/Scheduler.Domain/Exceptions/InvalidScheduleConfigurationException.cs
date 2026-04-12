namespace Scheduler.Domain.Exceptions;

/// <summary>
/// Excepción para ScheduleConfiguration Especificamente
/// </summary>
public class InvalidScheduleConfigurationException : Exception
{
    public InvalidScheduleConfigurationException(string message) 
        : base(message) { }

    public InvalidScheduleConfigurationException(string message, Exception innerException) 
        : base(message, innerException) { }
}