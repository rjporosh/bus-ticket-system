using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BusTicketing.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IOptions<SmsSettings> settings, IHttpClientFactory httpClientFactory, ILogger<SmsService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(string toPhoneNumber, string passengerName, string ticketNumber, string routeName, string busNumber, DateOnly travelDate, TimeOnly departureTime, string seatNumber, decimal fareAmount, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableNotifications)
            return;

        var message = $"Booking Confirmed\nTicket: {ticketNumber}\nRoute: {routeName}\nBus: {busNumber}\nDate: {travelDate:yyyy-MM-dd}\nDeparture: {departureTime:hh\\:mm}\nSeat: {seatNumber}\nFare: ৳{fareAmount:N2}\nPlease present this at boarding.";

        await SendAsync(toPhoneNumber, message, cancellationToken);
    }

    public async Task SendPaymentConfirmationAsync(string toPhoneNumber, string passengerName, string ticketNumber, decimal amount, PaymentMethod method, string transactionRef, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableNotifications)
            return;

        var message = $"Payment Successful\nTicket: {ticketNumber}\nAmount: ৳{amount:N2}\nMethod: {method}\nRef: {transactionRef}\nThank you for traveling with us.";

        await SendAsync(toPhoneNumber, message, cancellationToken);
    }

    public async Task SendPaymentFailureAsync(string toPhoneNumber, string passengerName, string ticketNumber, string reason, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableNotifications)
            return;

        var message = $"Payment Failed\nTicket: {ticketNumber}\nReason: {reason}\nPlease try again or contact support.";

        await SendAsync(toPhoneNumber, message, cancellationToken);
    }

    private async Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
            return;

        try
        {
            if (_settings.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaTwilioAsync(toPhoneNumber, message, cancellationToken);
            }
            else if (_settings.Provider.Equals("GsmGateway", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaGsmGatewayAsync(toPhoneNumber, message, cancellationToken);
            }
            else
            {
                _logger.LogWarning("SMS provider '{Provider}' is not configured. SMS not sent to {Phone}.", _settings.Provider, toPhoneNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}.", toPhoneNumber);
        }
    }

    private async Task SendViaTwilioAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        try
        {
            TwilioClient.Init(_settings.Twilio.AccountSid, _settings.Twilio.AuthToken);

            var to = new PhoneNumber(NormalizePhoneNumber(toPhoneNumber));
            var from = new PhoneNumber(_settings.Twilio.FromNumber);

            var result = await MessageResource.CreateAsync(
                to: to,
                from: from,
                body: message);

            _logger.LogInformation("SMS sent via Twilio. SID: {Sid}, Status: {Status}", result.Sid, result.Status);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Twilio API error: {Message}", ex.Message);
            throw;
        }
    }

    private async Task SendViaGsmGatewayAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsGateway");
        client.BaseAddress = new Uri(_settings.GsmGateway.BaseUrl);

        var request = new
        {
            api_key = _settings.GsmGateway.ApiKey,
            senderId = _settings.GsmGateway.SenderId,
            number = NormalizePhoneNumber(toPhoneNumber),
            message = message
        };

        var response = await client.PostAsJsonAsync("api/v1/send-sms", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GSM Gateway returned {StatusCode}: {Error}", response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("SMS sent via GSM Gateway to {Phone}.", toPhoneNumber);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (cleaned.StartsWith("0"))
            cleaned = "+88" + cleaned.Substring(1);
        else if (!cleaned.StartsWith("+"))
            cleaned = "+" + cleaned;

        return cleaned;
    }
}
