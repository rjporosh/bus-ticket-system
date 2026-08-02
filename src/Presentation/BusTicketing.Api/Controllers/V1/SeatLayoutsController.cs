using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Features.SeatLayouts;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buses/{busId:guid}/seat-layout")]
[Authorize]
[Produces("application/json")]
public class SeatLayoutsController : ControllerBase
{
    private readonly ISender _sender;
    public SeatLayoutsController(ISender sender) => _sender = sender;

    /// <summary>Gets the seat map (grid of seats with class/status) for a bus.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SeatLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid busId, CancellationToken cancellationToken)
        => (await _sender.Send(new GetSeatLayoutByBusIdQuery(busId), cancellationToken)).ToApiResult();

    /// <summary>Marks a seat in-service or out-of-service (e.g. a broken seat). Admin only.</summary>
    [HttpPatch("seats/{seatId:guid}/status")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetSeatStatus(Guid busId, Guid seatId, [FromBody] SetSeatStatusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new SetSeatServiceStatusCommand(seatId, request.IsActive), cancellationToken)).ToApiResult();

    /// <summary>Reclassifies a seat (Economy/Business/Sleeper). Admin only.</summary>
    [HttpPatch("seats/{seatId:guid}/class")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Reclassify(Guid busId, Guid seatId, [FromBody] ReclassifySeatRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new ReclassifySeatCommand(seatId, request.Class), cancellationToken)).ToApiResult();
}

public record SetSeatStatusRequest(bool IsActive);
public record ReclassifySeatRequest(SeatClass Class);
