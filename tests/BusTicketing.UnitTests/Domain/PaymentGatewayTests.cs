using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Domain;

public class PaymentGatewayTests
{
    [Fact]
    public void CreatePending_WithValidData_StartsInPendingState()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Bkash, "BKASH-TKT-1");

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Method.Should().Be(PaymentMethod.Bkash);
    }

    [Fact]
    public void CreatePending_WithBkashMethod_UsesCorrectMethod()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 1200m, PaymentMethod.Bkash, "BKASH-TKT-2");

        payment.Method.Should().Be(PaymentMethod.Bkash);
        payment.Amount.Should().Be(1200m);
    }

    [Fact]
    public void CreatePending_WithNagadMethod_UsesCorrectMethod()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 500m, PaymentMethod.Nagad, "NAGAD-TKT-1");

        payment.Method.Should().Be(PaymentMethod.Nagad);
    }

    [Fact]
    public void CreatePending_WithCardMethod_UsesCorrectMethod()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 1500m, PaymentMethod.Card, "CARD-TKT-1");

        payment.Method.Should().Be(PaymentMethod.Card);
    }

    [Fact]
    public void UpdateTransactionRef_WithValidRef_UpdatesTransactionRef()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Bkash, "OLD-REF");

        payment.UpdateTransactionRef("NEW-BKASH-REF-123");

        payment.TransactionRef.Should().Be("NEW-BKASH-REF-123");
    }

    [Fact]
    public void UpdateTransactionRef_WithEmptyRef_ThrowsDomainException()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Bkash, "OLD-REF");

        var act = () => payment.UpdateTransactionRef("");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetGatewayTransactionId_WithValidId_SetsGatewayTransactionId()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Bkash, "BKASH-TKT-1");

        payment.UpdateTransactionRef("GATEWAY-TRX-999");

        payment.TransactionRef.Should().Be("GATEWAY-TRX-999");
    }

    [Fact]
    public void Capture_FromPending_WithBkash_SetsCaptured()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Bkash, "BKASH-TKT-1");

        payment.Capture(DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Refund_FromCaptured_WithNagad_SetsRefunded()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Nagad, "NAGAD-TKT-1");
        payment.Capture(DateTimeOffset.UtcNow);

        payment.Refund(DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Fail_FromPending_WithCard_SetsFailedWithReason()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Card, "CARD-TKT-1");

        payment.Fail("Insufficient funds", DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Insufficient funds");
    }
}
