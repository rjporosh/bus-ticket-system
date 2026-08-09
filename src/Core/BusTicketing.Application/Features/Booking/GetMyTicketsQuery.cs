using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetMyTicketsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<TicketDto>>;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, PaginatedList<TicketDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyTicketsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PaginatedList<TicketDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var query =
            from t in _db.Tickets
                .Include(t => t.Schedule).ThenInclude(s => s.Bus)
                .Include(t => t.Schedule).ThenInclude(s => s.Route)
                .Include(t => t.Seat)
            join u in _db.Users on t.SoldByUserId equals u.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            where t.SoldByUserId == userId
            orderby t.SoldAtUtc descending
            select new TicketDto(
                t.Id, t.TicketNumber, t.ScheduleId, t.Schedule.Bus.Number, t.Schedule.Route.Name,
                t.SeatId, t.Seat.SeatNumber, t.TravelDate, t.Schedule.DepartureTime,
                t.PassengerName, t.MobileNumber, t.NidOrPassport, t.Gender, t.Age, t.Remarks,
                t.FareAmount, t.Status, seller != null ? seller.Username : "unknown", t.SoldAtUtc,
                t.CancellationReason, t.CancelledAtUtc, null, null);

        return PaginatedList<TicketDto>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
