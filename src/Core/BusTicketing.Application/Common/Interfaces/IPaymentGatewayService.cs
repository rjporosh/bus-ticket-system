using BusTicketing.Domain.Enums;

namespace BusTicketing.Application.Common.Interfaces;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> CreatePaymentAsync(Guid ticketId, decimal amount, PaymentMethod method, string customerPhone, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> QueryPaymentAsync(string transactionRef, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundAsync(string transactionRef, decimal? amount = null, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> CancelAsync(string transactionRef, CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(string payload, string signature, out string transactionRef);
}

public record PaymentGatewayResult(bool IsSuccess, string TransactionRef, string? Status, string? GatewayTransactionId, string? FailureReason, Dictionary<string, string>? Metadata = null);
