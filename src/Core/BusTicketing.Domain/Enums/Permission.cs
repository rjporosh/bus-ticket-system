namespace BusTicketing.Domain.Enums;

public enum Permission
{
    BookingSell = 1,
    BookingCancel = 2,
    BookingSearch = 3,
    BookingViewOwn = 4,
    DashboardView = 5,
    ScheduleManage = 6,
    BusManage = 7,
    RouteManage = 8,
    StationManage = 9,
    UserManage = 10,
    RoleManage = 11,
    PaymentManage = 12,
    PaymentCapture = 13,
    PaymentRefund = 14,
    PaymentFail = 15,
}
