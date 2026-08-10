using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record SellTicketCommand(
    Guid ScheduleId,
    Guid SeatId,
    DateOnly TravelDate,
    string PassengerName,
    string MobileNumber,
    decimal FareAmount,
    PaymentMethod PaymentMethod,
    string? NidOrPassport = null,
    string? Gender = null,
    int? Age = null,
    string? Remarks = null,
    string? Email = null) : IRequest<Result<TicketDto>>;

public class SellTicketCommandValidator : AbstractValidator<SellTicketCommand>
{
    public SellTicketCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.SeatId).NotEmpty();
        RuleFor(x => x.PassengerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FareAmount).GreaterThan(0);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

/// <summary>
/// Sells a ticket end-to-end: validates the trip runs on the requested date, validates
/// the seat belongs to that bus and is in service, prevents double-booking with both an
/// application-level pre-check and a DB unique-index backstop (a race between two booth
/// staff selling the same seat within milliseconds is caught by the index even if the
/// pre-check both requests observed passed), and captures a mock payment — all inside one
/// transaction, so a ticket is never left sold without a corresponding payment record or
/// vice versa.
/// </summary>
public class SellTicketCommandHandler : IRequestHandler<SellTicketCommand, Result<TicketDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailService _emailService;

    public SellTicketCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime, IAuditLogService auditLog, IEmailService emailService)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLog = auditLog;
        _emailService = emailService;
    }

    public async Task<Result<TicketDto>> Handle(SellTicketCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Bus)
            .Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            return Result.Failure<TicketDto>(Error.NotFound("Schedule was not found."));
        if (!schedule.RunsOn(request.TravelDate))
            return Result.Failure<TicketDto>(Error.Conflict($"This schedule does not run on {request.TravelDate:yyyy-MM-dd}."));

        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == request.SeatId, cancellationToken);
        if (seat is null)
            return Result.Failure<TicketDto>(Error.NotFound("Seat was not found."));
        if (!seat.IsActive)
            return Result.Failure<TicketDto>(Error.Conflict($"Seat {seat.SeatNumber} is out of service."));

        var seatBelongsToBus = await _db.SeatLayouts
            .AnyAsync(l => l.BusId == schedule.BusId && l.Seats.Any(s => s.Id == request.SeatId), cancellationToken);
        if (!seatBelongsToBus)
            return Result.Failure<TicketDto>(Error.Validation(new Dictionary<string, string[]>
            {
                ["seatId"] = new[] { "This seat does not belong to the scheduled bus." }
            }));

        // Application-level pre-check. The unique index on (ScheduleId, TravelDate, SeatId)
        // filtered to Status = Sold is the authoritative backstop for the race condition
        // this check alone cannot fully close — see DATABASE.md.
        var alreadySold = await _db.Tickets.AnyAsync(t =>
            t.ScheduleId == request.ScheduleId &&
            t.TravelDate == request.TravelDate &&
            t.SeatId == request.SeatId &&
            t.Status == TicketStatus.Sold,
            cancellationToken);

        if (alreadySold)
            return Result.Failure<TicketDto>(Error.Conflict($"Seat {seat.SeatNumber} is already sold for this trip."));

        var now = _dateTime.UtcNow;
        var ticketNumber = await GenerateTicketNumberAsync(request.TravelDate, cancellationToken);

        var ticket = Ticket.Sell(
            ticketNumber, request.ScheduleId, request.SeatId, request.TravelDate,
            request.PassengerName, request.MobileNumber, request.FareAmount,
            _currentUser.UserId ?? Guid.Empty, now,
            request.NidOrPassport, request.Gender, request.Age, request.Remarks);

        var payment = Payment.CreatePending(ticket.Id, request.FareAmount, request.PaymentMethod, $"MOCK-{ticketNumber}");
        payment.Capture(now); // mock gateway: always succeeds synchronously

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.Tickets.Add(ticket);
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index violation: another request won the race for this exact seat.
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<TicketDto>(Error.Conflict($"Seat {seat.SeatNumber} was just sold by another request. Please choose a different seat."));
        }

        await _auditLog.LogAsync("SellTicket", nameof(Ticket), ticket.Id.ToString(),
            $"Seat {seat.SeatNumber}, {request.PassengerName}, ৳{request.FareAmount}", cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendBookingConfirmationAsync(
                        request.Email, request.PassengerName, ticket.TicketNumber,
                        schedule.Route.Name, schedule.Bus.Number, request.TravelDate,
                        schedule.DepartureTime, seat.SeatNumber, request.FareAmount);
                }
                catch
                {
                    // Email failures are non-critical.
                }
            }, CancellationToken.None);
        }

        return Result.Success(new TicketDto(
            ticket.Id, ticket.TicketNumber, schedule.Id, schedule.Bus.Number, schedule.Route.Name,
            seat.Id, seat.SeatNumber, ticket.TravelDate, schedule.DepartureTime,
            ticket.PassengerName, ticket.MobileNumber, ticket.NidOrPassport, ticket.Gender, ticket.Age, ticket.Remarks,
            ticket.FareAmount, ticket.Status, _currentUser.Username ?? "unknown", ticket.SoldAtUtc,
            null, null, payment.Status, payment.TransactionRef));
    }

    /// <summary>
    /// Format TKT-YYYYMMDD-XXXX, sequential per travel date, matching the reference brief
    /// exactly. Known limitation: under very high concurrent sell volume on the same date,
    /// two requests could theoretically read the same count before either commits, causing
    /// a ticket-number collision (caught by the unique index, surfaced as a generic
    /// conflict). At this system's scale (a handful of booths, tens of sales/minute) this
    /// is acceptable; a production-hardened version would use a DB sequence per date
    /// instead of a COUNT query — noted in ROADMAP.md.
    /// </summary>
    private async Task<string> GenerateTicketNumberAsync(DateOnly travelDate, CancellationToken cancellationToken)
    {
        var datePart = travelDate.ToString("yyyyMMdd");
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var counter = await _db.TicketNumberCounters
                .FirstOrDefaultAsync(c => c.CounterDate == travelDate, cancellationToken);

            if (counter is null)
            {
                counter = TicketNumberCounter.Create(travelDate);
                _db.TicketNumberCounters.Add(counter);
            }

            var nextNumber = counter.Next();
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return $"TKT-{datePart}-{nextNumber:D4}";
            }
            catch (DbUpdateConcurrencyException)
            {
                await Task.Delay(10, cancellationToken);
            }
        }

        throw new BusinessRuleViolationException("Could not generate a unique ticket number due to high concurrency. Please retry.");
    }
}
