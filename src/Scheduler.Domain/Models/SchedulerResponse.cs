namespace Scheduler.Domain.Models;

public readonly struct SchedulerResponse
{
    public bool IsSuccess { get; }
    public DateTimeOffset? NextExecutionTime => (NextExecutionTimes != null && NextExecutionTimes.Any()) ? NextExecutionTimes.First() : null;
    public IEnumerable<DateTimeOffset> NextExecutionTimes { get; }
    public string Description { get; }
    public string ErrorMessage { get; }

    public SchedulerResponse(IEnumerable<DateTimeOffset> executions, string description)
    {
        IsSuccess = true;
        NextExecutionTimes = executions?.OrderBy(e => e).ToList() ?? new List<DateTimeOffset>();
        Description = description;
        ErrorMessage = string.Empty;
    }

    public SchedulerResponse(DateTimeOffset execution, string description) : this(new[] { execution }, description) { }

    public SchedulerResponse(string errorMessage)
    {
        IsSuccess = false;
        NextExecutionTimes = Enumerable.Empty<DateTimeOffset>();
        Description = string.Empty;
        ErrorMessage = errorMessage;
    }
}