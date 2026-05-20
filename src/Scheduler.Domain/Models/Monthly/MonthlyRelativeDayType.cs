namespace Scheduler.Domain.Models.Monthly;

public enum MonthlyRelativeDayType
{
    // mapped 0-6 exactly the same as "System.DayOfWeek" to facilitate the conversion
    Sunday      = 0,
    Monday      = 1,
    Tuesday     = 2,
    Wednesday   = 3,
    Thursday    = 4,
    Friday      = 5,
    Saturday    = 6,
    // Special types
    Day         = 10,
    Weekday     = 11,
    WeekendDay  = 12

}