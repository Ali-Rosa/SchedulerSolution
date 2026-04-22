
namespace Scheduler.Domain.Models
{
    public readonly struct SchedulerResponse
    {
        public bool IsSuccess { get; }
        public DateTimeOffset? NextExecutionTime { get; } 
        public string Description { get; }
        public string ErrorMessage { get; }

        public SchedulerResponse(DateTimeOffset? nextExecutionTime, string description)
        {
            IsSuccess = true;
            NextExecutionTime = nextExecutionTime;
            Description = description;
            ErrorMessage = string.Empty;
        }

        public SchedulerResponse(string errorMessage)
        {
            IsSuccess = false;
            NextExecutionTime = null;
            Description = string.Empty;
            ErrorMessage = errorMessage;
        }
    }
}
