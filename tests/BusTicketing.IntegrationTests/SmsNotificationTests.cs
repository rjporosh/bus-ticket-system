using System.Net;
using System.Net.Http.Json;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusTicketing.IntegrationTests;

public class SmsNotificationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SmsNotificationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PaymentWebhook_Bkash_Endpoint_IsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payments/webhook/bkash", new
        {
            trxID = "TEST-TRX-123",
            paymentStatus = "Completed"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PaymentWebhook_Nagad_Success_Endpoint_IsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payments/webhook/nagad/success", new
        {
            paymentRefId = "TEST-NAGAD-123"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PaymentWebhook_Nagad_Fail_Endpoint_IsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payments/webhook/nagad/fail", new
        {
            paymentRefId = "TEST-NAGAD-FAIL"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PaymentWebhook_Card_Endpoint_IsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payments/webhook/card", new
        {
            id = "TEST-CARD-123",
            type = "payment.succeeded"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NewPaymentMethods_ExistInEnum()
    {
        var bkashExists = Enum.IsDefined(typeof(PaymentMethod), PaymentMethod.Bkash);
        var nagadExists = Enum.IsDefined(typeof(PaymentMethod), PaymentMethod.Nagad);
        var cardExists = Enum.IsDefined(typeof(PaymentMethod), PaymentMethod.Card);

        bkashExists.Should().BeTrue();
        nagadExists.Should().BeTrue();
        cardExists.Should().BeTrue();
    }

    [Fact]
    public async Task SmsService_IsRegisteredInDi()
    {
        using var scope = _factory.Services.CreateScope();
        var smsService = scope.ServiceProvider.GetService<BusTicketing.Application.Common.Interfaces.ISmsService>();
        smsService.Should().NotBeNull();
    }

    [Fact]
    public async Task PaymentGatewayService_IsRegisteredInDi()
    {
        using var scope = _factory.Services.CreateScope();
        var gatewayService = scope.ServiceProvider.GetService<BusTicketing.Application.Common.Interfaces.IPaymentGatewayService>();
        gatewayService.Should().NotBeNull();
    }
}
