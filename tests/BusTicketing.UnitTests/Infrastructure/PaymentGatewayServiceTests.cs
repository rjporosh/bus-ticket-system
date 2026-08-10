using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using BusTicketing.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net.Http;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Infrastructure;

public class PaymentGatewayServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentGatewayService> _logger;
    private readonly PaymentGatewaySettings _settings;

    public PaymentGatewayServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<PaymentGatewayService>>();
        _settings = new PaymentGatewaySettings
        {
            Provider = "Mock",
            EnableRealGateway = false
        };
    }

    [Fact]
    public async Task CreatePaymentAsync_WithMockProvider_ReturnsSuccessWithMockRef()
    {
        var service = CreateService();

        var result = await service.CreatePaymentAsync(Guid.NewGuid(), 1000m, PaymentMethod.Bkash, "+8801712345678");

        result.IsSuccess.Should().BeTrue();
        result.TransactionRef.Should().StartWith("MOCK-");
        result.Status.Should().Be("Captured");
    }

    [Fact]
    public async Task CreatePaymentAsync_WithMockProvider_GeneratesUniqueRefs()
    {
        var service = CreateService();
        var ticketId = Guid.NewGuid();

        var result1 = await service.CreatePaymentAsync(ticketId, 1000m, PaymentMethod.Bkash, "+8801712345678");
        await Task.Delay(1100);
        var result2 = await service.CreatePaymentAsync(ticketId, 1000m, PaymentMethod.Bkash, "+8801712345678");

        result1.TransactionRef.Should().NotBe(result2.TransactionRef);
    }

    [Fact]
    public async Task QueryPaymentAsync_WithMockProvider_ReturnsCaptured()
    {
        var service = CreateService();
        var ref1 = "MOCK-REF-123";

        var result = await service.QueryPaymentAsync(ref1);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be("Captured");
    }

    [Fact]
    public async Task RefundAsync_WithMockProvider_ReturnsRefunded()
    {
        var service = CreateService();
        var ref1 = "MOCK-REF-123";

        var result = await service.RefundAsync(ref1);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be("Refunded");
    }

    [Fact]
    public async Task CancelAsync_WithMockProvider_ReturnsCancelled()
    {
        var service = CreateService();
        var ref1 = "MOCK-REF-123";

        var result = await service.CancelAsync(ref1);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CreatePaymentAsync_WithUnsupportedMethod_ReturnsFailure()
    {
        _settings.EnableRealGateway = true;
        _settings.Provider = "Bkash";
        var service = CreateService();

        var result = await service.CreatePaymentAsync(Guid.NewGuid(), 1000m, (PaymentMethod)999, "+8801712345678");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("not supported");
    }

    private PaymentGatewayService CreateService()
    {
        var options = Options.Create(_settings);
        return new PaymentGatewayService(options, _httpClientFactory, _logger);
    }
}
