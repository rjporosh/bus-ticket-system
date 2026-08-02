using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Buses;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buses")]
[Authorize]
[Produces("application/json")]
public class BusesController : ControllerBase
{
    private readonly ISender _sender;
    public BusesController(ISender sender) => _sender = sender;

    /// <summary>Lists buses with optional search and active-status filters, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<BusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<BusDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetBusesQuery(search, isActive, pageNumber, pageSize), cancellationToken));

    /// <summary>Gets a single bus by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new GetBusByIdQuery(id), cancellationToken)).ToApiResult();

    /// <summary>Creates a new bus and auto-generates its seat layout (Rows x Columns). Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(BusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateBusCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(b => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/buses/{b.Id}", b));
    }

    /// <summary>Updates a bus's number and operator name. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(BusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateBusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new UpdateBusCommand(id, request.Number, request.OperatorName), cancellationToken)).ToApiResult();

    /// <summary>Activates or deactivates a bus (deactivated buses cannot be scheduled). Admin only.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetActiveStatus(Guid id, [FromBody] SetActiveStatusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new SetBusActiveCommand(id, request.IsActive), cancellationToken)).ToApiResult();
}

public record UpdateBusRequest(string Number, string OperatorName);
