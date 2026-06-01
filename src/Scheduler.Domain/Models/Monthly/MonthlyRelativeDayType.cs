using System.ComponentModel;

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

    [Description("day")]
    Day         = 10,
    [Description("weekday")]
    Weekday     = 11,
    [Description("weekend day")]
    WeekendDay  = 12

}