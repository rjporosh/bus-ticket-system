using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BusTicketing.Infrastructure.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly PaymentGatewaySettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentGatewayService> _logger;

    public PaymentGatewayService(IOptions<PaymentGatewaySettings> settings, IHttpClientFactory httpClientFactory, ILogger<PaymentGatewayService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PaymentGatewayResult> CreatePaymentAsync(Guid ticketId, decimal amount, PaymentMethod method, string customerPhone, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRealGateway || _settings.Provider == "Mock")
        {
            var mockRef = $"MOCK-{ticketId:N}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            return new PaymentGatewayResult(true, mockRef, "Captured", mockRef, null);
        }

        try
        {
            return method switch
            {
                PaymentMethod.Bkash => await CreateBkashPaymentAsync(ticketId, amount, customerPhone, cancellationToken),
                PaymentMethod.Nagad => await CreateNagadPaymentAsync(ticketId, amount, customerPhone, cancellationToken),
                PaymentMethod.Card => await CreateCardPaymentAsync(ticketId, amount, customerPhone, cancellationToken),
                _ => throw new NotSupportedException($"Payment method {method} is not supported for real gateway processing.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {Method} payment for ticket {TicketId}.", method, ticketId);
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Gateway error: {ex.Message}");
        }
    }

    public async Task<PaymentGatewayResult> QueryPaymentAsync(string transactionRef, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRealGateway || _settings.Provider == "Mock")
            return new PaymentGatewayResult(true, transactionRef, "Captured", transactionRef, null);

        try
        {
            return _settings.Provider.ToLower() switch
            {
                "bkash" => await QueryBkashPaymentAsync(transactionRef, cancellationToken),
                "nagad" => await QueryNagadPaymentAsync(transactionRef, cancellationToken),
                "card" => await QueryCardPaymentAsync(transactionRef, cancellationToken),
                _ => throw new NotSupportedException($"Provider {_settings.Provider} is not supported for querying.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query payment {TransactionRef}.", transactionRef);
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Query error: {ex.Message}");
        }
    }

    public async Task<PaymentGatewayResult> RefundAsync(string transactionRef, decimal? amount = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRealGateway || _settings.Provider == "Mock")
            return new PaymentGatewayResult(true, transactionRef, "Refunded", transactionRef, null);

        try
        {
            return _settings.Provider.ToLower() switch
            {
                "bkash" => await RefundBkashAsync(transactionRef, amount, cancellationToken),
                "nagad" => await RefundNagadAsync(transactionRef, amount, cancellationToken),
                "card" => await RefundCardAsync(transactionRef, amount, cancellationToken),
                _ => throw new NotSupportedException($"Provider {_settings.Provider} is not supported for refunds.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {TransactionRef}.", transactionRef);
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Refund error: {ex.Message}");
        }
    }

    public async Task<PaymentGatewayResult> CancelAsync(string transactionRef, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRealGateway || _settings.Provider == "Mock")
            return new PaymentGatewayResult(true, transactionRef, "Cancelled", transactionRef, null);

        try
        {
            return _settings.Provider.ToLower() switch
            {
                "bkash" => await CancelBkashAsync(transactionRef, cancellationToken),
                "nagad" => await CancelNagadAsync(transactionRef, cancellationToken),
                "card" => await CancelCardAsync(transactionRef, cancellationToken),
                _ => throw new NotSupportedException($"Provider {_settings.Provider} is not supported for cancellation.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel payment {TransactionRef}.", transactionRef);
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Cancel error: {ex.Message}");
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature, out string transactionRef)
    {
        transactionRef = string.Empty;

        if (_settings.Provider.Equals("Bkash", StringComparison.OrdinalIgnoreCase))
        {
            return VerifyBkashSignature(payload, signature, out transactionRef);
        }
        else if (_settings.Provider.Equals("Nagad", StringComparison.OrdinalIgnoreCase))
        {
            return VerifyNagadSignature(payload, signature, out transactionRef);
        }

        return true;
    }

    private async Task<PaymentGatewayResult> CreateBkashPaymentAsync(Guid ticketId, decimal amount, string customerPhone, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("BkashGateway");
        client.BaseAddress = new Uri(_settings.Bkash.BaseUrl);

        var token = await GetBkashAccessTokenAsync(client, cancellationToken);

        var request = new
        {
            mode = "0011",
            payerReference = customerPhone,
            callbackURL = $"http://localhost:{_settings.Bkash.WebhookPort}/api/v1/payments/webhook/bkash",
            amount = amount.ToString("F2"),
            currency = "BDT",
            intent = "sale",
            merchantInvoiceNumber = ticketId.ToString("N")
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "tokenized/checkout/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", token);
        httpRequest.Headers.Add("X-APP-Key", _settings.Bkash.AppKey);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("bKash create payment failed: {Content}", content);
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash error: {content}");
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("statusCode", out var statusCode) && statusCode.GetString() == "0000")
        {
            var bkashTrxId = root.GetProperty("trxID").GetString() ?? string.Empty;
            var paymentUrl = root.GetProperty("bkashURL").GetString() ?? string.Empty;
            return new PaymentGatewayResult(true, ticketId.ToString("N"), "Pending", bkashTrxId, null, new Dictionary<string, string>
            {
                ["PaymentUrl"] = paymentUrl
            });
        }

        var message = root.TryGetProperty("statusMessage", out var sm) ? sm.GetString() : "Unknown bKash error";
        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash error: {message}");
    }

    private async Task<PaymentGatewayResult> QueryBkashPaymentAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("BkashGateway");
        client.BaseAddress = new Uri(_settings.Bkash.BaseUrl);

        var token = await GetBkashAccessTokenAsync(client, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"tokenized/checkout/payment/status/{transactionRef}");
        request.Headers.Add("Authorization", token);
        request.Headers.Add("X-APP-Key", _settings.Bkash.AppKey);

        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash query failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("statusCode", out var statusCode) && statusCode.GetString() == "0000")
        {
            var status = root.GetProperty("transactionStatus").GetString() ?? "Unknown";
            return new PaymentGatewayResult(true, transactionRef, status, transactionRef, null);
        }

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash query error: {content}");
    }

    private async Task<PaymentGatewayResult> RefundBkashAsync(string transactionRef, decimal? amount, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("BkashGateway");
        client.BaseAddress = new Uri(_settings.Bkash.BaseUrl);

        var token = await GetBkashAccessTokenAsync(client, cancellationToken);

        var request = new
        {
            trxID = transactionRef,
            amount = (amount ?? 0).ToString("F2"),
            reason = "Customer refund"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "tokenized/checkout/refund")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", token);
        httpRequest.Headers.Add("X-APP-Key", _settings.Bkash.AppKey);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash refund failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("statusCode", out var statusCode) && statusCode.GetString() == "0000")
            return new PaymentGatewayResult(true, transactionRef, "Refunded", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash refund error: {content}");
    }

    private async Task<PaymentGatewayResult> CancelBkashAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("BkashGateway");
        client.BaseAddress = new Uri(_settings.Bkash.BaseUrl);

        var token = await GetBkashAccessTokenAsync(client, cancellationToken);

        var request = new { trxID = transactionRef };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "tokenized/checkout/cancel")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", token);
        httpRequest.Headers.Add("X-APP-Key", _settings.Bkash.AppKey);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash cancel failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("statusCode", out var statusCode) && statusCode.GetString() == "0000")
            return new PaymentGatewayResult(true, transactionRef, "Cancelled", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"bKash cancel error: {content}");
    }

    private async Task<string> GetBkashAccessTokenAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Bkash.Username}:{_settings.Bkash.Password}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "tokenized/checkout/token/grant")
        {
            Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Add("Authorization", $"Basic {credentials}");
        request.Headers.Add("X-APP-Key", _settings.Bkash.AppKey);

        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to get bKash token: {content}");

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id_token").GetString() ?? string.Empty;
    }

    private bool VerifyBkashSignature(string payload, string signature, out string transactionRef)
    {
        transactionRef = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("trxID", out var trxId))
            {
                transactionRef = trxId.GetString() ?? string.Empty;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<PaymentGatewayResult> CreateNagadPaymentAsync(Guid ticketId, decimal amount, string customerPhone, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("NagadGateway");
        client.BaseAddress = new Uri(_settings.Nagad.BaseUrl);

        var request = new
        {
            merchantId = _settings.Nagad.MerchantId,
            orderId = ticketId.ToString("N"),
            amount = amount.ToString("F2"),
            currency = "BDT",
            customerMobileNo = customerPhone,
            successUrl = $"http://localhost:{_settings.Nagad.WebhookPort}/api/v1/payments/webhook/nagad/success",
            failUrl = $"http://localhost:{_settings.Nagad.WebhookPort}/api/v1/payments/webhook/nagad/fail",
            cancelUrl = $"http://localhost:{_settings.Nagad.WebhookPort}/api/v1/payments/webhook/nagad/cancel"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "remote-payment-gateway-request")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad error: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "Success")
        {
            var paymentRef = root.GetProperty("paymentRefNo").GetString() ?? string.Empty;
            var redirectUrl = root.GetProperty("redirectUrl").GetString() ?? string.Empty;
            return new PaymentGatewayResult(true, ticketId.ToString("N"), "Pending", paymentRef, null, new Dictionary<string, string>
            {
                ["RedirectUrl"] = redirectUrl
            });
        }

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad error: {content}");
    }

    private async Task<PaymentGatewayResult> QueryNagadPaymentAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("NagadGateway");
        client.BaseAddress = new Uri(_settings.Nagad.BaseUrl);

        var request = new { paymentRefId = transactionRef };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "remote-payment-gateway-request/verify")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad query failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "Success")
        {
            var paymentStatus = root.GetProperty("paymentStatus").GetString() ?? "Unknown";
            return new PaymentGatewayResult(true, transactionRef, paymentStatus, transactionRef, null);
        }

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad query error: {content}");
    }

    private async Task<PaymentGatewayResult> RefundNagadAsync(string transactionRef, decimal? amount, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("NagadGateway");
        client.BaseAddress = new Uri(_settings.Nagad.BaseUrl);

        var request = new
        {
            paymentRefId = transactionRef,
            refundAmount = (amount ?? 0).ToString("F2"),
            reason = "Customer refund"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "refund")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad refund failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "Success")
            return new PaymentGatewayResult(true, transactionRef, "Refunded", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad refund error: {content}");
    }

    private async Task<PaymentGatewayResult> CancelNagadAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("NagadGateway");
        client.BaseAddress = new Uri(_settings.Nagad.BaseUrl);

        var request = new { paymentRefId = transactionRef };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "cancel")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad cancel failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "Success")
            return new PaymentGatewayResult(true, transactionRef, "Cancelled", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Nagad cancel error: {content}");
    }

    private bool VerifyNagadSignature(string payload, string signature, out string transactionRef)
    {
        transactionRef = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("paymentRefId", out var refId))
            {
                transactionRef = refId.GetString() ?? string.Empty;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<PaymentGatewayResult> CreateCardPaymentAsync(Guid ticketId, decimal amount, string customerPhone, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("CardGateway");
        client.BaseAddress = new Uri(_settings.CardGateway.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.CardGateway.ApiKey}");

        var request = new
        {
            merchantId = _settings.CardGateway.MerchantId,
            orderId = ticketId.ToString("N"),
            amount = amount.ToString("F2"),
            currency = "BDT",
            customerPhone = customerPhone,
            returnUrl = "http://localhost:4200/payment/return",
            cancelUrl = "http://localhost:4200/payment/cancel",
            webhookUrl = "http://localhost:5000/api/v1/payments/webhook/card"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/payments")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card gateway error: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("id", out var id) && root.TryGetProperty("status", out var status))
        {
            var paymentId = id.GetString() ?? string.Empty;
            var paymentStatus = status.GetString() ?? "Pending";
            return new PaymentGatewayResult(true, ticketId.ToString("N"), paymentStatus, paymentId, null);
        }

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card gateway error: {content}");
    }

    private async Task<PaymentGatewayResult> QueryCardPaymentAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("CardGateway");
        client.BaseAddress = new Uri(_settings.CardGateway.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.CardGateway.ApiKey}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/payments/{transactionRef}");
        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card query failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status))
            return new PaymentGatewayResult(true, transactionRef, status.GetString() ?? "Unknown", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card query error: {content}");
    }

    private async Task<PaymentGatewayResult> RefundCardAsync(string transactionRef, decimal? amount, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("CardGateway");
        client.BaseAddress = new Uri(_settings.CardGateway.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.CardGateway.ApiKey}");

        var request = new
        {
            amount = (amount ?? 0).ToString("F2"),
            reason = "Customer refund"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/payments/{transactionRef}/refund")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card refund failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "succeeded")
            return new PaymentGatewayResult(true, transactionRef, "Refunded", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card refund error: {content}");
    }

    private async Task<PaymentGatewayResult> CancelCardAsync(string transactionRef, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("CardGateway");
        client.BaseAddress = new Uri(_settings.CardGateway.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.CardGateway.ApiKey}");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/payments/{transactionRef}/cancel");
        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card cancel failed: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetString() == "canceled")
            return new PaymentGatewayResult(true, transactionRef, "Cancelled", transactionRef, null);

        return new PaymentGatewayResult(false, string.Empty, "Failed", string.Empty, $"Card cancel error: {content}");
    }

    private async Task<string> EncryptNagadRequestAsync(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return JsonSerializer.Serialize(fields);
    }
}
