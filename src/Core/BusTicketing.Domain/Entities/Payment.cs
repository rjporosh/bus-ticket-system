using BusTicketing.Domain.Common;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// A mock payment record against a Ticket. No real payment processor is integrated
/// (matches the brief's "Mock Payment" module) — <see cref="Capture"/> simulates a
/// gateway confirming the charge synchronously, which is sufficient to demonstrate
/// the full sell-ticket-with-payment flow without a third-party dependency.
/// </summary>
public class Payment : BaseEntity
{
    public Guid TicketId { get; private set; }
    public Ticket Ticket { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string TransactionRef { get; private set; } = default!;
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment() { } // EF Core

    public static Payment CreatePending(Guid ticketId, decimal amount, PaymentMethod method, string transactionRef)
    {
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(transactionRef))
            throw new DomainException("Transaction reference is required.");

        return new Payment
        {
            TicketId = ticketId,
            Amount = amount,
            Method = method,
            TransactionRef = transactionRef,
            Status = PaymentStatus.Pending
        };
    }

    /// <summary>Simulates the mock gateway confirming the charge.</summary>
    public void Capture(DateTimeOffset processedAtUtc)
    {
        if (Status != PaymentStatus.Pending)
            throw new BusinessRuleViolationException($"Payment {TransactionRef} is not in a capturable state ({Status}).");

        Status = PaymentStatus.Captured;
        ProcessedAtUtc = processedAtUtc;
    }

    public void Fail(string reason, DateTimeOffset processedAtUtc)
    {
        if (Status != PaymentStatus.Pending)
            throw new BusinessRuleViolationException($"Payment {TransactionRef} is not in a failable state ({Status}).");

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = processedAtUtc;
    }

    public void Refund(DateTimeOffset processedAtUtc)
    {
        if (Status != PaymentStatus.Captured)
            throw new BusinessRuleViolationException($"Payment {TransactionRef} must be Captured before it can be refunded.");

        Status = PaymentStatus.Refunded;
        ProcessedAtUtc = processedAtUtc;
    }
}
