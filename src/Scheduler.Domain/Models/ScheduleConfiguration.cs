namespace Scheduler.Domain.Models;

public record ScheduleConfiguration(
    bool Enabled,
    ScheduleType Type,
    DateTimeOffset? ExecutionDateTimeUtc,
    OccursType Occurs,
    int Every,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc,
    string TimeZoneId
)
{
    public static ScheduleConfiguration DefaultOnce() =>
        new(
            Enabled: true,
            Type: ScheduleType.Once,
            ExecutionDateTimeUtc: null,
            Occurs: OccursType.Daily,
            Every: 1,
            StartDateUtc: null,
            EndDateUtc: null,
            TimeZoneId: TimeZoneInfo.Utc.Id
        );

    public static ScheduleConfiguration DefaultRecurring() =>
        new(
            Enabled: true,
            Type: ScheduleType.Recurring,
            ExecutionDateTimeUtc: null,
            Occurs: OccursType.Daily,
            Every: 1,
            StartDateUtc: DateTimeOffset.UtcNow,
            EndDateUtc: null,
            TimeZoneId: TimeZoneInfo.Utc.Id
        );
}