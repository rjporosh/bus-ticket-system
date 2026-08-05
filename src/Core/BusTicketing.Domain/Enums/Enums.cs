namespace BusTicketing.Domain.Enums;

/// <summary>Built-in system roles. Additional custom roles may be created by an Admin at runtime.</summary>
public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string BoothStaff = "BoothStaff";
    public const string Customer = "Customer";
}

public enum SeatClass
{
    Economy = 0,
    Business = 1,
    Sleeper = 2
}

public enum SeatStatusDefault
{
    /// <summary>The template status of a seat on the bus's layout, independent of any specific trip.</summary>
    Active = 0,
    OutOfService = 1
}

public enum ScheduleStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum DayOfWeekFlag
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    Daily = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}
