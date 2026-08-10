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

public class SellTicketCommandHandler : IRequestHandler<SellTicketCommand, Result<TicketDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPaymentGatewayService _paymentGateway;

    public SellTicketCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime, IAuditLogService auditLog, IEmailService emailService, ISmsService smsService, IPaymentGatewayService paymentGateway)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLog = auditLog;
        _emailService = emailService;
        _smsService = smsService;
        _paymentGateway = paymentGateway;
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

        var paymentTransactionRef = $"MOCK-{ticketNumber}";
        Payment payment;

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            payment = Payment.CreatePending(ticket.Id, request.FareAmount, request.PaymentMethod, paymentTransactionRef);

            if (request.PaymentMethod is PaymentMethod.Cash)
            {
                payment.Capture(now);
            }
            else
            {
                var gatewayResult = await _paymentGateway.CreatePaymentAsync(ticket.Id, request.FareAmount, request.PaymentMethod, request.MobileNumber, cancellationToken);

                if (gatewayResult.IsSuccess)
                {
                    paymentTransactionRef = gatewayResult.TransactionRef;
                    payment.UpdateTransactionRef(paymentTransactionRef);

                    if (gatewayResult.Status == "Captured" || gatewayResult.Status == "succeeded")
                    {
                        payment.Capture(now);
                    }
                }
                else if (!string.IsNullOrEmpty(gatewayResult.FailureReason))
                {
                    payment.Fail(gatewayResult.FailureReason, now);
                }
            }

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
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
                }
            }, CancellationToken.None);
        }

        if (!string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _smsService.SendBookingConfirmationAsync(
                        request.MobileNumber, request.PassengerName, ticket.TicketNumber,
                        schedule.Route.Name, schedule.Bus.Number, request.TravelDate,
                        schedule.DepartureTime, seat.SeatNumber, request.FareAmount);

                    if (payment.Status == PaymentStatus.Captured)
                    {
                        await _smsService.SendPaymentConfirmationAsync(
                            request.MobileNumber, request.PassengerName, ticket.TicketNumber,
                            request.FareAmount, request.PaymentMethod, payment.TransactionRef);
                    }
                    else if (payment.Status == PaymentStatus.Failed)
                    {
                        await _smsService.SendPaymentFailureAsync(
                            request.MobileNumber, request.PassengerName, ticket.TicketNumber,
                            payment.FailureReason ?? "Unknown error");
                    }
                }
                catch
                {
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
