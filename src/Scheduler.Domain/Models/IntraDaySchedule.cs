namespace Scheduler.Domain.Models
{
    public sealed record IntraDaySchedule(
        IntraDayFrequencyUnit Unit,
        int Every,
        TimeOnly StartTime,
        TimeOnly EndTime
    );
}
