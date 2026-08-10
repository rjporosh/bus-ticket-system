using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetPaymentsQuery(
    Guid? TicketId = null,
    PaymentStatus? Status = null,
    PaymentMethod? Method = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<PaymentDto>>;

public record PaymentDto(
    Guid Id,
    Guid TicketId,
    string TicketNumber,
    string PassengerName,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string TransactionRef,
    DateTimeOffset? ProcessedAtUtc,
    string? FailureReason);

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, PaginatedList<PaymentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPaymentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PaginatedList<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Payments
            .Include(p => p.Ticket)
            .AsQueryable();

        if (request.TicketId.HasValue)
            query = query.Where(p => p.TicketId == request.TicketId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.Method.HasValue)
            query = query.Where(p => p.Method == request.Method.Value);

        if (request.FromDate.HasValue)
            query = query.Where(p => p.Ticket.TravelDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(p => p.Ticket.TravelDate <= request.ToDate.Value);

        var projected = query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PaymentDto(
                p.Id, p.TicketId, p.Ticket.TicketNumber, p.Ticket.PassengerName,
                p.Amount, p.Method, p.Status, p.TransactionRef, p.ProcessedAtUtc, p.FailureReason));

        return PaginatedList<PaymentDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record CapturePaymentCommand(Guid PaymentId) : IRequest<Result<PaymentDto>>;

public class CapturePaymentCommandHandler : IRequestHandler<CapturePaymentCommand, Result<PaymentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPaymentGatewayService _paymentGateway;

    public CapturePaymentCommandHandler(IApplicationDbContext db, IDateTimeProvider dateTime, IPaymentGatewayService paymentGateway)
    {
        _db = db;
        _dateTime = dateTime;
        _paymentGateway = paymentGateway;
    }

    public async Task<Result<PaymentDto>> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment was not found."));

        try
        {
            if (payment.Method is PaymentMethod.Cash)
            {
                payment.Capture(_dateTime.UtcNow);
            }
            else
            {
                var gatewayResult = await _paymentGateway.QueryPaymentAsync(payment.TransactionRef, cancellationToken);
                if (gatewayResult.IsSuccess && (gatewayResult.Status == "Captured" || gatewayResult.Status == "succeeded"))
                {
                    payment.Capture(_dateTime.UtcNow);
                }
                else if (!string.IsNullOrEmpty(gatewayResult.FailureReason))
                {
                    payment.Fail(gatewayResult.FailureReason, _dateTime.UtcNow);
                }
                else
                {
                    return Result.Failure<PaymentDto>(Error.Conflict("Payment is not yet confirmed by the gateway."));
                }
            }
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<PaymentDto>(Error.Conflict(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentDto(
            payment.Id, payment.TicketId, payment.Ticket.TicketNumber, payment.Ticket.PassengerName,
            payment.Amount, payment.Method, payment.Status, payment.TransactionRef, payment.ProcessedAtUtc, payment.FailureReason));
    }
}

public record RefundPaymentCommand(Guid PaymentId) : IRequest<Result<PaymentDto>>;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, Result<PaymentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPaymentGatewayService _paymentGateway;

    public RefundPaymentCommandHandler(IApplicationDbContext db, IDateTimeProvider dateTime, IPaymentGatewayService paymentGateway)
    {
        _db = db;
        _dateTime = dateTime;
        _paymentGateway = paymentGateway;
    }

    public async Task<Result<PaymentDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment was not found."));

        try
        {
            if (payment.Method is PaymentMethod.Cash)
            {
                payment.Refund(_dateTime.UtcNow);
            }
            else
            {
                var gatewayResult = await _paymentGateway.RefundAsync(payment.TransactionRef, payment.Amount, cancellationToken);
                if (gatewayResult.IsSuccess)
                {
                    payment.Refund(_dateTime.UtcNow);
                }
                else
                {
                    return Result.Failure<PaymentDto>(Error.Conflict(gatewayResult.FailureReason ?? "Refund failed at gateway."));
                }
            }
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<PaymentDto>(Error.Conflict(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentDto(
            payment.Id, payment.TicketId, payment.Ticket.TicketNumber, payment.Ticket.PassengerName,
            payment.Amount, payment.Method, payment.Status, payment.TransactionRef, payment.ProcessedAtUtc, payment.FailureReason));
    }
}

public record FailPaymentCommand(Guid PaymentId, string Reason) : IRequest<Result<PaymentDto>>;

public class FailPaymentCommandHandler : IRequestHandler<FailPaymentCommand, Result<PaymentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPaymentGatewayService _paymentGateway;

    public FailPaymentCommandHandler(IApplicationDbContext db, IDateTimeProvider dateTime, IPaymentGatewayService paymentGateway)
    {
        _db = db;
        _dateTime = dateTime;
        _paymentGateway = paymentGateway;
    }

    public async Task<Result<PaymentDto>> Handle(FailPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment was not found."));

        try
        {
            if (payment.Method is PaymentMethod.Cash)
            {
                payment.Fail(request.Reason, _dateTime.UtcNow);
            }
            else
            {
                var gatewayResult = await _paymentGateway.CancelAsync(payment.TransactionRef, cancellationToken);
                if (gatewayResult.IsSuccess)
                {
                    payment.Fail(request.Reason, _dateTime.UtcNow);
                }
                else
                {
                    return Result.Failure<PaymentDto>(Error.Conflict(gatewayResult.FailureReason ?? "Cancel failed at gateway."));
                }
            }
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<PaymentDto>(Error.Conflict(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentDto(
            payment.Id, payment.TicketId, payment.Ticket.TicketNumber, payment.Ticket.PassengerName,
            payment.Amount, payment.Method, payment.Status, payment.TransactionRef, payment.ProcessedAtUtc, payment.FailureReason));
    }
}
