namespace BusTicketing.Domain.Enums;

public enum TicketStatus
{
    Sold = 0,
    Cancelled = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Captured = 1,
    Failed = 2,
    Refunded = 3
}

public enum PaymentMethod
{
    Cash = 0,
    MockCard = 1,
    MockMobileBanking = 2,
    Bkash = 3,
    Nagad = 4,
    Card = 5
}
