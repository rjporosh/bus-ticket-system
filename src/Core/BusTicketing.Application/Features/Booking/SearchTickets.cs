using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public enum TicketSearchField
{
    TicketNumber = 0,
    MobileNumber = 1
}

public record SearchTicketsQuery(
    TicketSearchField? SearchBy,
    string? SearchText,
    DateOnly? TravelDate,
    Guid? RouteId,
    TicketStatus? Status,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<TicketDto>>;

public class SearchTicketsQueryHandler : IRequestHandler<SearchTicketsQuery, PaginatedList<TicketDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchTicketsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PaginatedList<TicketDto>> Handle(SearchTicketsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from t in _db.Tickets
                .Include(t => t.Schedule).ThenInclude(s => s.Bus)
                .Include(t => t.Schedule).ThenInclude(s => s.Route)
                .Include(t => t.Seat)
            join u in _db.Users on t.SoldByUserId equals u.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            select new { Ticket = t, SellerUsername = seller != null ? seller.Username : "unknown" };

        if (!string.IsNullOrWhiteSpace(request.SearchText) && request.SearchBy is not null)
        {
            var term = request.SearchText.Trim();
            query = request.SearchBy switch
            {
                TicketSearchField.TicketNumber => query.Where(x => x.Ticket.TicketNumber == term),
                TicketSearchField.MobileNumber => query.Where(x => x.Ticket.MobileNumber == term),
                _ => query
            };
        }

        if (request.TravelDate.HasValue)
            query = query.Where(x => x.Ticket.TravelDate == request.TravelDate.Value);

        if (request.RouteId.HasValue)
            query = query.Where(x => x.Ticket.Schedule.RouteId == request.RouteId.Value);

        if (request.Status.HasValue)
            query = query.Where(x => x.Ticket.Status == request.Status.Value);

        var projected = query
            .OrderByDescending(x => x.Ticket.SoldAtUtc)
            .Select(x => new TicketDto(
                x.Ticket.Id, x.Ticket.TicketNumber, x.Ticket.ScheduleId, x.Ticket.Schedule.Bus.Number, x.Ticket.Schedule.Route.Name,
                x.Ticket.SeatId, x.Ticket.Seat.SeatNumber, x.Ticket.TravelDate, x.Ticket.Schedule.DepartureTime,
                x.Ticket.PassengerName, x.Ticket.MobileNumber, x.Ticket.NidOrPassport, x.Ticket.Gender, x.Ticket.Age, x.Ticket.Remarks,
                x.Ticket.FareAmount, x.Ticket.Status, x.SellerUsername, x.Ticket.SoldAtUtc,
                x.Ticket.CancellationReason, x.Ticket.CancelledAtUtc, null, null));

        return PaginatedList<TicketDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
