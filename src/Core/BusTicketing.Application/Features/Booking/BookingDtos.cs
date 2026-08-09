using BusTicketing.Domain.Enums;

namespace BusTicketing.Application.Features.Booking;

public record TicketDto(
    Guid Id,
    string TicketNumber,
    Guid ScheduleId,
    string BusNumber,
    string RouteName,
    Guid SeatId,
    string SeatNumber,
    DateOnly TravelDate,
    TimeOnly DepartureTime,
    string PassengerName,
    string MobileNumber,
    string? NidOrPassport,
    string? Gender,
    string? Remarks,
    decimal FareAmount,
    TicketStatus Status,
    string SoldByUsername,
    DateTimeOffset SoldAtUtc,
    string? CancellationReason,
    DateTimeOffset? CancelledAtUtc,
    PaymentStatus? PaymentStatus,
    string? PaymentTransactionRef);

public record SeatAvailabilityDto(
    Guid SeatId,
    string SeatNumber,
    string RowLabel,
    int ColumnNumber,
    SeatClass Class,
    bool IsInService,
    bool IsSold,
    bool IsDriver = false,
    int? VisualRow = null,
    int? VisualCol = null,
    string? PassengerName = null,
    string? PassengerGender = null);

public record DashboardSummaryDto(
    DateOnly Date,
    int TotalSeats,
    int SoldSeats,
    int AvailableSeats,
    decimal TotalSales,
    List<RouteSalesDto> RouteWiseSales,
    List<BusSeatStatusDto> BusWiseSeatStatus);

public record RouteSalesDto(string RouteName, int SoldTickets, int AvailableSeats, decimal TotalSales);

public record BusSeatStatusDto(string BusNumber, string RouteName, TimeOnly DepartureTime, int AvailableSeats, int TotalSeats);
