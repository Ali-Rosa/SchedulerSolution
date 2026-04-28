namespace Scheduler.Domain.Models
{ 
    public record ScheduleConfiguration(
        bool Enabled,
        ScheduleType Type,
        DateTimeOffset? ExecutionDateTimeLocal,
        OccursType Occurs,
        int Every,
        DateTimeOffset? StartDateLocal,
        DateTimeOffset? EndDateLocal,
        string TimeZoneId,
        IntraDaySchedule? IntraDay,
        WeeklySchedule? Weekly

    )
    {
        public static ScheduleConfiguration DefaultOnce() =>
            new(
                Enabled: true,
                Type: ScheduleType.Once,
                ExecutionDateTimeLocal: null,
                Occurs: OccursType.Daily,
                Every: 0,
                StartDateLocal: null,
                EndDateLocal: null,
                TimeZoneId: TimeZoneInfo.Utc.Id,
                IntraDay: null,
                Weekly: null
            );

        public static ScheduleConfiguration DefaultRecurring() =>
            new(
                Enabled: true,
                Type: ScheduleType.Recurring,
                ExecutionDateTimeLocal: null,
                Occurs: OccursType.Daily,
                Every: 1,
                StartDateLocal: null,
                EndDateLocal: null,
                TimeZoneId: TimeZoneInfo.Utc.Id,
                IntraDay: null,
                Weekly: null
            );
    }

}