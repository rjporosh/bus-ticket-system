using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Localization;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Authorize(Policy = "Permission:PaymentManage")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IStringLocalizer<SharedResources> _localizer;
    public PaymentsController(ISender sender, IStringLocalizer<SharedResources> localizer)
    {
        _sender = sender;
        _localizer = localizer;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<PaymentDto>>> GetPayments(
        [FromQuery] Guid? ticketId, [FromQuery] PaymentStatus? status, [FromQuery] PaymentMethod? method,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetPaymentsQuery(ticketId, status, method, fromDate, toDate, pageNumber, pageSize), cancellationToken));

    [HttpPost("{id:guid}/capture")]
    [Authorize(Policy = "Permission:PaymentCapture")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Capture(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new CapturePaymentCommand(id), cancellationToken)).ToApiResult(localizer: _localizer);

    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "Permission:PaymentRefund")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Refund(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new RefundPaymentCommand(id), cancellationToken)).ToApiResult(localizer: _localizer);

    [HttpPost("{id:guid}/fail")]
    [Authorize(Policy = "Permission:PaymentFail")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Fail(Guid id, CancellationToken cancellationToken)
    {
        var reason = "Failed by admin";
        return (await _sender.Send(new FailPaymentCommand(id, reason), cancellationToken)).ToApiResult(localizer: _localizer);
    }
}
