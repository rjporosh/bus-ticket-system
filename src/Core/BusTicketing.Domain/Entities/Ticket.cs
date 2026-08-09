using BusTicketing.Domain.Common;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// A sold (or cancelled) seat reservation for one schedule on one travel date.
/// The uniqueness of (ScheduleId, TravelDate, SeatId) — enforced both here at the
/// application level and by a DB unique index — is what prevents double-booking; see
/// DATABASE.md for the full explanation of why both layers check.
/// </summary>
public class Ticket : BaseEntity
{
    public string TicketNumber { get; private set; } = default!;

    public Guid ScheduleId { get; private set; }
    public Schedule Schedule { get; private set; } = default!;

    public Guid SeatId { get; private set; }
    public Seat Seat { get; private set; } = default!;

    public DateOnly TravelDate { get; private set; }

    public string PassengerName { get; private set; } = default!;
    public string MobileNumber { get; private set; } = default!;
    public string? NidOrPassport { get; private set; }
    public string? Gender { get; private set; }
    public int? Age { get; private set; }
    public string? Remarks { get; private set; }

    public decimal FareAmount { get; private set; }
    public TicketStatus Status { get; private set; }

    public Guid SoldByUserId { get; private set; }
    public DateTimeOffset SoldAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    private Ticket() { } // EF Core

    public static Ticket Sell(
        string ticketNumber,
        Guid scheduleId,
        Guid seatId,
        DateOnly travelDate,
        string passengerName,
        string mobileNumber,
        decimal fareAmount,
        Guid soldByUserId,
        DateTimeOffset soldAtUtc,
        string? nidOrPassport = null,
        string? gender = null,
        int? age = null,
        string? remarks = null)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new DomainException("Ticket number is required.");
        if (string.IsNullOrWhiteSpace(passengerName))
            throw new DomainException("Passenger name is required.");
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new DomainException("Passenger mobile number is required.");
        if (fareAmount <= 0)
            throw new DomainException("Fare amount must be greater than zero.");

        return new Ticket
        {
            TicketNumber = ticketNumber,
            ScheduleId = scheduleId,
            SeatId = seatId,
            TravelDate = travelDate,
            PassengerName = passengerName.Trim(),
            MobileNumber = mobileNumber.Trim(),
            NidOrPassport = nidOrPassport,
            Gender = gender,
            Age = age,
            Remarks = remarks,
            FareAmount = fareAmount,
            Status = TicketStatus.Sold,
            SoldByUserId = soldByUserId,
            SoldAtUtc = soldAtUtc
        };
    }

    /// <summary>
    /// Cancels the ticket, freeing the seat for resale on this trip. Only a Sold
    /// ticket for a trip that hasn't already departed can be cancelled — matching
    /// the reference brief's rule "Booth staff can cancel a ticket before journey time."
    /// </summary>
    public void Cancel(string reason, Guid cancelledByUserId, DateTimeOffset cancelledAtUtc, DateTimeOffset departureAtUtc)
    {
        if (Status == TicketStatus.Cancelled)
            throw new BusinessRuleViolationException($"Ticket {TicketNumber} is already cancelled.");
        if (cancelledAtUtc >= departureAtUtc)
            throw new BusinessRuleViolationException($"Ticket {TicketNumber} cannot be cancelled after the journey's departure time.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required.");

        Status = TicketStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = reason.Trim();
        CancelledByUserId = cancelledByUserId;
    }
}
