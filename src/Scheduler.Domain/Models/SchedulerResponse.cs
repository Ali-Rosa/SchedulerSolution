namespace Scheduler.Domain.Models;

public readonly struct SchedulerResponse
{
    public bool IsSuccess { get; }

    public DateTimeOffset? NextExecutionTime => (NextsExecutionsTimes != null && NextsExecutionsTimes.Any()) ? NextsExecutionsTimes.First() : null;

    public IEnumerable<DateTimeOffset> NextsExecutionsTimes { get; }

    public string Description { get; }

    public string ErrorMessage { get; }

    public SchedulerResponse(IEnumerable<DateTimeOffset> executions, string description)
    {
        IsSuccess = true;
        NextsExecutionsTimes = executions?.OrderBy(e => e).ToList() ?? new List<DateTimeOffset>();
        Description = description;
        ErrorMessage = string.Empty;
    }

    public SchedulerResponse(DateTimeOffset execution, string description) : this(new[] { execution }, description) { }

    public SchedulerResponse(string errorMessage)
    {
        IsSuccess = false;
        NextsExecutionsTimes = Enumerable.Empty<DateTimeOffset>();
        Description = string.Empty;
        ErrorMessage = errorMessage;
    }
}