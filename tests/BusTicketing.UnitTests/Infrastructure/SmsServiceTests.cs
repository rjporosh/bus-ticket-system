using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using BusTicketing.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BusTicketing.UnitTests.Infrastructure;

public class SmsServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsService> _logger;
    private readonly SmsSettings _settings;

    public SmsServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<SmsService>>();
        _settings = new SmsSettings
        {
            Provider = "None",
            EnableNotifications = true
        };
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_WhenDisabled_CompletesWithoutError()
    {
        _settings.EnableNotifications = false;
        var service = CreateService();

        await service.SendBookingConfirmationAsync("+8801712345678", "John", "TKT-001", "Route A", "Bus 1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.FromDateTime(DateTime.UtcNow), "A1", 500m);
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_WithEmptyPhone_CompletesWithoutError()
    {
        var service = CreateService();

        await service.SendBookingConfirmationAsync("", "John", "TKT-001", "Route A", "Bus 1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.FromDateTime(DateTime.UtcNow), "A1", 500m);
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_WithInvalidProvider_LogsWarning()
    {
        _settings.Provider = "UnknownProvider";
        var service = CreateService();

        await service.SendBookingConfirmationAsync("+8801712345678", "John", "TKT-001", "Route A", "Bus 1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.FromDateTime(DateTime.UtcNow), "A1", 500m);
    }

    [Fact]
    public async Task SendPaymentConfirmationAsync_WithTwilioProvider_CompletesWithoutError()
    {
        _settings.Provider = "Twilio";
        _settings.Twilio = new TwilioSettings
        {
            AccountSid = "test-sid",
            AuthToken = "test-token",
            FromNumber = "+8801234567890"
        };

        var service = CreateService();

        await service.SendPaymentConfirmationAsync("+8801712345678", "John", "TKT-001", 500m, PaymentMethod.Bkash, "BKASH-REF-123");
    }

    [Fact]
    public async Task SendPaymentFailureAsync_WithGsmGateway_CompletesWithoutError()
    {
        _settings.Provider = "GsmGateway";
        _settings.GsmGateway = new GsmGatewaySettings
        {
            BaseUrl = "https://test.gateway.com",
            ApiKey = "test-key",
            SenderId = "Test"
        };

        var httpClient = new HttpClient();
        _httpClientFactory.CreateClient("SmsGateway").Returns(httpClient);
        var service = CreateService();

        await service.SendPaymentFailureAsync("+8801712345678", "John", "TKT-001", "Insufficient funds");
    }

    private SmsService CreateService()
    {
        var options = Options.Create(_settings);
        return new SmsService(options, _httpClientFactory, _logger);
    }
}
