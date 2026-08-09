using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Domain;

public class PaymentTests
{
    [Fact]
    public void CreatePending_WithValidData_StartsInPendingState()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Cash, "MOCK-TKT-1");

        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void CreatePending_WithZeroAmount_ThrowsDomainException()
    {
        var act = () => Payment.CreatePending(Guid.NewGuid(), 0m, PaymentMethod.Cash, "MOCK-TKT-1");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Capture_FromPending_SetsStatusToCaptured()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Cash, "MOCK-TKT-1");

        payment.Capture(DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Capture_WhenAlreadyCaptured_ThrowsBusinessRuleViolationException()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Cash, "MOCK-TKT-1");
        payment.Capture(DateTimeOffset.UtcNow);

        var act = () => payment.Capture(DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Refund_FromCaptured_SetsStatusToRefunded()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Cash, "MOCK-TKT-1");
        payment.Capture(DateTimeOffset.UtcNow);

        payment.Refund(DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Refund_WithoutFirstCapturing_ThrowsBusinessRuleViolationException()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.Cash, "MOCK-TKT-1");

        var act = () => payment.Refund(DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*Captured*");
    }

    [Fact]
    public void Fail_FromPending_SetsStatusToFailedWithReason()
    {
        var payment = Payment.CreatePending(Guid.NewGuid(), 800m, PaymentMethod.MockCard, "MOCK-TKT-1");

        payment.Fail("Card declined", DateTimeOffset.UtcNow);

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Card declined");
    }
}
