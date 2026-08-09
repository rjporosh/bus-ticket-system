using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record SellTicketsCommand(
    Guid ScheduleId,
    DateOnly TravelDate,
    List<SellTicketItem> Items,
    string? Remarks = null) : IRequest<Result<List<TicketDto>>>;

public record SellTicketItem(
    Guid SeatId,
    string PassengerName,
    string MobileNumber,
    decimal FareAmount,
    PaymentMethod PaymentMethod,
    string? NidOrPassport = null,
    string? Gender = null,
    int? Age = null);

public class SellTicketsCommandValidator : AbstractValidator<SellTicketsCommand>
{
    public SellTicketsCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.TravelDate).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().Must(items => items.Count > 0 && items.Count <= 10)
            .WithMessage("You can book between 1 and 10 seats at a time.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SeatId).NotEmpty();
            item.RuleFor(i => i.PassengerName).NotEmpty().MaximumLength(150);
            item.RuleFor(i => i.MobileNumber).NotEmpty().MaximumLength(20);
            item.RuleFor(i => i.FareAmount).GreaterThan(0);
        });
    }
}

public class SellTicketsCommandHandler : IRequestHandler<SellTicketsCommand, Result<List<TicketDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;

    public SellTicketsCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime, IAuditLogService auditLog)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLog = auditLog;
    }

    public async Task<Result<List<TicketDto>>> Handle(SellTicketsCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Bus)
            .Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            return Result.Failure<List<TicketDto>>(Error.NotFound("Schedule was not found."));
        if (!schedule.RunsOn(request.TravelDate))
            return Result.Failure<List<TicketDto>>(Error.Conflict($"This schedule does not run on {request.TravelDate:yyyy-MM-dd}."));

        var seatIds = request.Items.Select(i => i.SeatId).Distinct().ToHashSet();
        var seats = await _db.Seats
            .Where(s => seatIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!seats.TryGetValue(item.SeatId, out var seat))
                return Result.Failure<List<TicketDto>>(Error.NotFound($"Seat {item.SeatId} was not found."));
            if (!seat.IsActive)
                return Result.Failure<List<TicketDto>>(Error.Conflict($"Seat {seat.SeatNumber} is out of service."));
        }

        var seatBelongsToBus = await _db.SeatLayouts
            .AnyAsync(l => l.BusId == schedule.BusId && l.Seats.Any(s => seatIds.Contains(s.Id)), cancellationToken);
        if (!seatBelongsToBus)
            return Result.Failure<List<TicketDto>>(Error.Validation(new Dictionary<string, string[]>
            {
                ["seatId"] = new[] { "One or more seats do not belong to the scheduled bus." }
            }));

        var alreadySoldSeatIds = await _db.Tickets
            .Where(t => t.ScheduleId == request.ScheduleId
                        && t.TravelDate == request.TravelDate
                        && t.Status == TicketStatus.Sold
                        && seatIds.Contains(t.SeatId))
            .Select(t => t.SeatId)
            .ToListAsync(cancellationToken);

        if (alreadySoldSeatIds.Count > 0)
        {
            var soldLabels = string.Join(", ", alreadySoldSeatIds.Select(id => seats.TryGetValue(id, out var s) ? s.SeatNumber : id.ToString()));
            return Result.Failure<List<TicketDto>>(Error.Conflict($"Seat(s) already sold for this trip: {soldLabels}."));
        }

        var now = _dateTime.UtcNow;
        var tickets = new List<Ticket>();
        var payments = new List<Payment>();
        var resultDtos = new List<TicketDto>();

        foreach (var item in request.Items)
        {
            var ticketNumber = await GenerateTicketNumberAsync(request.TravelDate, cancellationToken);

            var ticket = Ticket.Sell(
                ticketNumber, request.ScheduleId, item.SeatId, request.TravelDate,
                item.PassengerName, item.MobileNumber, item.FareAmount,
                _currentUser.UserId ?? Guid.Empty, now,
                item.NidOrPassport, item.Gender, item.Age, request.Remarks);

            var payment = Payment.CreatePending(ticket.Id, item.FareAmount, item.PaymentMethod, $"MOCK-{ticketNumber}");
            payment.Capture(now);

            tickets.Add(ticket);
            payments.Add(payment);
        }

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.Tickets.AddRange(tickets);
            _db.Payments.AddRange(payments);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<List<TicketDto>>(Error.Conflict("One or more seats were just sold by another request. Please choose different seats."));
        }

        for (var i = 0; i < tickets.Count; i++)
        {
            var ticket = tickets[i];
            var seat = seats[ticket.SeatId];
            var payment = payments[i];

            await _auditLog.LogAsync("SellTickets", nameof(Ticket), ticket.Id.ToString(),
                $"Seat {seat.SeatNumber}, {ticket.PassengerName}, ৳{ticket.FareAmount}", cancellationToken);

            resultDtos.Add(new TicketDto(
                ticket.Id, ticket.TicketNumber, schedule.Id, schedule.Bus.Number, schedule.Route.Name,
                ticket.SeatId, seat.SeatNumber, ticket.TravelDate, schedule.DepartureTime,
                ticket.PassengerName, ticket.MobileNumber, ticket.NidOrPassport, ticket.Gender, ticket.Age, ticket.Remarks,
                ticket.FareAmount, ticket.Status, _currentUser.Username ?? "unknown", ticket.SoldAtUtc,
                null, null, payment.Status, payment.TransactionRef));
        }

        return Result.Success(resultDtos);
    }

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
