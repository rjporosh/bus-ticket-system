using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record CancelTicketCommand(Guid TicketId, string Reason) : IRequest<Result<TicketDto>>;

public class CancelTicketCommandValidator : AbstractValidator<CancelTicketCommand>
{
    public CancelTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CancelTicketCommandHandler : IRequestHandler<CancelTicketCommand, Result<TicketDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;

    public CancelTicketCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime, IAuditLogService auditLog)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLog = auditLog;
    }

    public async Task<Result<TicketDto>> Handle(CancelTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Schedule).ThenInclude(s => s.Bus)
            .Include(t => t.Schedule).ThenInclude(s => s.Route)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
            return Result.Failure<TicketDto>(Error.NotFound($"Ticket {request.TicketId} was not found."));

        var now = _dateTime.UtcNow;
        // Naive UTC combination of travel date + scheduled departure time: acceptable for
        // an MVP operating in a single timezone context (see ROADMAP.md for the timezone
        // hardening item once multi-region operation is in scope).
        var departureAtUtc = new DateTimeOffset(
            ticket.TravelDate.ToDateTime(ticket.Schedule.DepartureTime), TimeSpan.Zero);

        try
        {
            ticket.Cancel(request.Reason, _currentUser.UserId ?? Guid.Empty, now, departureAtUtc);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<TicketDto>(Error.Conflict(ex.Message));
        }
        catch (DomainException ex)
        {
            return Result.Failure<TicketDto>(Error.Validation(new Dictionary<string, string[]> { ["reason"] = new[] { ex.Message } }));
        }

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.TicketId == ticket.Id, cancellationToken);
        if (payment is not null && payment.Status == PaymentStatus.Captured)
            payment.Refund(now);

        await _db.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync("CancelTicket", nameof(Ticket), ticket.Id.ToString(), request.Reason, cancellationToken);

        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == ticket.SoldByUserId, cancellationToken);

        return Result.Success(new TicketDto(
            ticket.Id, ticket.TicketNumber, ticket.ScheduleId, ticket.Schedule.Bus.Number, ticket.Schedule.Route.Name,
            ticket.SeatId, ticket.Seat.SeatNumber, ticket.TravelDate, ticket.Schedule.DepartureTime,
            ticket.PassengerName, ticket.MobileNumber, ticket.NidOrPassport, ticket.Gender, ticket.Age, ticket.Remarks,
            ticket.FareAmount, ticket.Status, seller?.Username ?? "unknown", ticket.SoldAtUtc,
            ticket.CancellationReason, ticket.CancelledAtUtc, payment?.Status, payment?.TransactionRef));
    }
}
