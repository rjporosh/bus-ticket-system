using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Schedules;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/schedules")]
[Authorize]
[Produces("application/json")]
public class SchedulesController : ControllerBase
{
    private readonly ISender _sender;
    public SchedulesController(ISender sender) => _sender = sender;

    /// <summary>Public: lists recurring schedules with optional bus/route/status filters, paginated.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedList<ScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ScheduleDto>>> GetAll(
        [FromQuery] Guid? busId, [FromQuery] Guid? routeId, [FromQuery] ScheduleStatus? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetSchedulesQuery(busId, routeId, status, pageNumber, pageSize), cancellationToken));

    /// <summary>Public: resolves recurring schedules into concrete trips for a specific travel date (e.g. "today's trips").</summary>
    [HttpGet("trips")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TripDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TripDto>>> GetTrips(
        [FromQuery] DateOnly travelDate, [FromQuery] Guid? routeId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTripsForDateQuery(travelDate, routeId), cancellationToken));

    /// <summary>Public: searches trips by origin/destination station and travel date.</summary>
    [HttpGet("search/trips")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedList<Application.Features.Schedules.TripDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<Application.Features.Schedules.TripDto>>> SearchTrips(
        [FromQuery] DateOnly travelDate, [FromQuery] Guid? originStationId, [FromQuery] Guid? destinationStationId,
        [FromQuery] string? originStationName, [FromQuery] string? destinationStationName,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new SearchTripsQuery(travelDate, originStationId, destinationStationId, originStationName, destinationStationName, pageNumber, pageSize), cancellationToken));

    /// <summary>Public: gets a single schedule by id.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
        => (await _sender.Send(new GetScheduleByIdQuery(id), cancellationToken)).ToApiResult();

    /// <summary>Creates a new recurring schedule for a bus on a route. Rejects overlapping bus/time conflicts with 409. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateScheduleCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(s => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/schedules/{s.Id}", s));
    }

    /// <summary>Updates a schedule's timing, fare and recurrence. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new UpdateScheduleCommand(
                id, request.DepartureTime, request.ArrivalTime, request.DaysOfWeek,
                request.EffectiveFrom, request.EffectiveTo, request.FareAmount),
            cancellationToken)).ToApiResult();

    /// <summary>Cancels or reactivates a schedule. Admin only.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetStatus(Guid id, [FromBody] SetScheduleStatusRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new SetScheduleStatusCommand(id, request.Cancel), cancellationToken)).ToApiResult();
}

public record UpdateScheduleRequest(TimeOnly DepartureTime, TimeOnly ArrivalTime, DayOfWeekFlag DaysOfWeek, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal FareAmount);
public record SetScheduleStatusRequest(bool Cancel);
