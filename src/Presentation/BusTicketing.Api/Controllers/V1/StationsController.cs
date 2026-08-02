using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Stations;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stations")]
[Authorize]
[Produces("application/json")]
public class StationsController : ControllerBase
{
    private readonly ISender _sender;
    public StationsController(ISender sender) => _sender = sender;

    /// <summary>Lists stations with optional search and active-status filters, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<StationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<StationDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetStationsQuery(search, isActive, pageNumber, pageSize), cancellationToken));

    /// <summary>Gets a single station by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new GetStationByIdQuery(id), cancellationToken)).ToApiResult();

    /// <summary>Creates a new station. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateStationCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(s => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/stations/{s.Id}", s));
    }

    /// <summary>Updates a station. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateStationRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new UpdateStationCommand(id, request.Name, request.City, request.Address), cancellationToken)).ToApiResult();

    /// <summary>Activates or deactivates a station. Admin only.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetActiveStatus(Guid id, [FromBody] SetActiveStatusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new SetStationActiveCommand(id, request.IsActive), cancellationToken)).ToApiResult();
}

public record UpdateStationRequest(string Name, string City, string? Address);
