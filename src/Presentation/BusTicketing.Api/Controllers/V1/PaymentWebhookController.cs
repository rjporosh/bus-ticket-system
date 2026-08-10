using Asp.Versioning;
using BusTicketing.Api.Authorization;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/payments/webhook")]
public class PaymentWebhookController : ControllerBase
{
    private readonly PaymentGatewaySettings _settings;
    private readonly IServiceProvider _serviceProvider;

    public PaymentWebhookController(IOptions<PaymentGatewaySettings> settings, IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("bkash")]
    [Consumes("application/json")]
    public async Task<IActionResult> BkashWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var signature = Request.Headers["X-Bkash-Signature"].ToString();
        if (!VerifyBkashSignature(payload, signature, out var transactionRef))
            return BadRequest(new { status = "Invalid signature" });

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentWebhookController>>();

        try
        {
            var status = payload.GetProperty("paymentStatus").GetString();
            var gatewayTrxId = payload.GetProperty("trxID").GetString();

            logger.LogInformation("Received bKash webhook. TransactionRef: {Ref}, Status: {Status}", transactionRef, status);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.TransactionRef == transactionRef, cancellationToken);
            if (payment is null)
            {
                logger.LogWarning("Payment not found for bKash webhook transactionRef: {Ref}", transactionRef);
                return NotFound(new { status = "Payment not found" });
            }

            if (status == "Completed" || status == "Authorized")
            {
                payment.Capture(dateTime.UtcNow);
                payment.UpdateTransactionRef(gatewayTrxId ?? transactionRef);
            }
            else if (status == "Failed" || status == "Cancelled")
            {
                payment.Fail($"bKash payment {status}", dateTime.UtcNow);
            }

            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { status = "Processed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing bKash webhook.");
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }

    [HttpPost("nagad/success")]
    [HttpPost("nagad/fail")]
    [HttpPost("nagad/cancel")]
    [Consumes("application/json")]
    public async Task<IActionResult> NagadWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var status = HttpContext.Request.Path.Value?.Contains("success") == true ? "Completed" :
                     HttpContext.Request.Path.Value?.Contains("fail") == true ? "Failed" : "Cancelled";

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentWebhookController>>();

        try
        {
            var paymentRef = payload.GetProperty("paymentRefId").GetString();

            logger.LogInformation("Received Nagad webhook. PaymentRef: {Ref}, Status: {Status}", paymentRef, status);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.TransactionRef == paymentRef, cancellationToken);
            if (payment is null)
            {
                logger.LogWarning("Payment not found for Nagad webhook paymentRef: {Ref}", paymentRef);
                return NotFound(new { status = "Payment not found" });
            }

            if (status == "Completed")
            {
                payment.Capture(dateTime.UtcNow);
            }
            else
            {
                payment.Fail($"Nagad payment {status}", dateTime.UtcNow);
            }

            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { status = "Processed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Nagad webhook.");
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }

    [HttpPost("card")]
    [Consumes("application/json")]
    public async Task<IActionResult> CardWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var signature = Request.Headers["X-Card-Signature"].ToString();
        if (!VerifyCardSignature(payload, signature))
            return BadRequest(new { status = "Invalid signature" });

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentWebhookController>>();

        try
        {
            var paymentId = payload.GetProperty("id").GetString();
            var eventType = payload.GetProperty("type").GetString();

            logger.LogInformation("Received Card webhook. PaymentId: {Id}, Type: {Type}", paymentId, eventType);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.TransactionRef == paymentId, cancellationToken);
            if (payment is null)
            {
                logger.LogWarning("Payment not found for Card webhook paymentId: {Id}", paymentId);
                return NotFound(new { status = "Payment not found" });
            }

            if (eventType == "payment.succeeded")
            {
                payment.Capture(dateTime.UtcNow);
            }
            else if (eventType == "payment.failed" || eventType == "payment.canceled")
            {
                payment.Fail($"Card payment {eventType}", dateTime.UtcNow);
            }

            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { status = "Processed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Card webhook.");
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }

    private bool VerifyBkashSignature(JsonElement payload, string signature, out string transactionRef)
    {
        transactionRef = string.Empty;
        try
        {
            if (payload.TryGetProperty("trxID", out var trxId))
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

    private bool VerifyCardSignature(JsonElement payload, string signature)
    {
        return true;
    }
}
