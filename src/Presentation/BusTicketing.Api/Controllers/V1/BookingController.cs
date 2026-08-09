using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/booking")]
[Authorize]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly ISender _sender;
    public BookingController(ISender sender) => _sender = sender;

    /// <summary>Gets the seat map for a schedule on a specific travel date, with sold seats flagged.</summary>
    [HttpGet("schedules/{scheduleId:guid}/seats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SeatAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAvailableSeats(Guid scheduleId, [FromQuery] DateOnly travelDate, CancellationToken cancellationToken)
        => (await _sender.Send(new GetAvailableSeatsQuery(scheduleId, travelDate), cancellationToken)).ToApiResult();

    /// <summary>
    /// Sells a ticket for one seat on one schedule/date, capturing a mock payment in the
    /// same transaction. Returns 409 if the seat was already sold for this trip (including
    /// the race-condition case caught by the DB unique index).
    /// </summary>
    [HttpPost("tickets")]
    [Authorize(Policy = "Permission:BookingSell")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SellTicket([FromBody] SellTicketCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(t => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/booking/tickets/{t.Id}", t));
    }

    /// <summary>Sells multiple seats in one transaction for the same schedule/date.</summary>
    [HttpPost("tickets/batch")]
    [Authorize(Policy = "Permission:BookingSell")]
    [ProducesResponseType(typeof(List<TicketDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SellTickets([FromBody] SellTicketsCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(t => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/booking/tickets/batch", t));
    }

    /// <summary>Cancels a sold ticket before its journey's departure time, freeing the seat and refunding the mock payment.</summary>
    [HttpPost("tickets/{ticketId:guid}/cancel")]
    [Authorize(Policy = "Permission:BookingCancel")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CancelTicket(Guid ticketId, [FromBody] CancelTicketRequest request, CancellationToken cancellationToken)
        => (await _sender.Send(new CancelTicketCommand(ticketId, request.Reason), cancellationToken)).ToApiResult();

    /// <summary>Searches tickets by ticket number, mobile number, travel date, route or status, paginated.</summary>
    [HttpGet("tickets")]
    [Authorize(Policy = "Permission:BookingSearch")]
    [ProducesResponseType(typeof(PaginatedList<TicketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<TicketDto>>> Search(
        [FromQuery] TicketSearchField? searchBy, [FromQuery] string? searchText, [FromQuery] DateOnly? travelDate,
        [FromQuery] Guid? routeId, [FromQuery] TicketStatus? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new SearchTicketsQuery(searchBy, searchText, travelDate, routeId, status, pageNumber, pageSize), cancellationToken));

    /// <summary>Returns the current customer's own bookings, paginated.</summary>
    [HttpGet("my-tickets")]
    [Authorize(Policy = "Permission:BookingViewOwn")]
    [ProducesResponseType(typeof(PaginatedList<TicketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<TicketDto>>> GetMyTickets(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetMyTicketsQuery(pageNumber, pageSize), cancellationToken));
}

public record CancelTicketRequest(string Reason);
