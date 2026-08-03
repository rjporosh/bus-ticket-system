using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Routes;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/routes")]
[Authorize]
[Produces("application/json")]
public class RoutesController : ControllerBase
{
    private readonly ISender _sender;
    public RoutesController(ISender sender) => _sender = sender;

    /// <summary>Lists routes with optional search, origin/destination and active-status filters, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<RouteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<RouteDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] Guid? originStationId, [FromQuery] Guid? destinationStationId,
        [FromQuery] bool? isActive, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetRoutesQuery(search, originStationId, destinationStationId, isActive, pageNumber, pageSize), cancellationToken));

    /// <summary>Gets a single route by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new GetRouteByIdQuery(id), cancellationToken)).ToApiResult();

    /// <summary>Creates a new route between two stations. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateRouteCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(r => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/routes/{r.Id}", r));
    }

    /// <summary>Updates a route's name, distance and duration. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateRouteRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new UpdateRouteCommand(id, request.Name, request.DistanceKm, request.EstimatedDurationMinutes), cancellationToken)).ToApiResult();

    /// <summary>Activates or deactivates a route. Admin only.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetActiveStatus(Guid id, [FromBody] SetActiveStatusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new SetRouteActiveCommand(id, request.IsActive), cancellationToken)).ToApiResult();
}

public record UpdateRouteRequest(string Name, decimal DistanceKm, int EstimatedDurationMinutes);
