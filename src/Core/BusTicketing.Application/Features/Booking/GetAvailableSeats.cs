using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetAvailableSeatsQuery(Guid ScheduleId, DateOnly TravelDate) : IRequest<Result<List<SeatAvailabilityDto>>>;

public class GetAvailableSeatsQueryHandler : IRequestHandler<GetAvailableSeatsQuery, Result<List<SeatAvailabilityDto>>>
{
    private readonly IApplicationDbContext _db;
    public GetAvailableSeatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<SeatAvailabilityDto>>> Handle(GetAvailableSeatsQuery request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);
        if (schedule is null)
            return Result.Failure<List<SeatAvailabilityDto>>(Error.NotFound("Schedule was not found."));

        var layout = await _db.SeatLayouts
            .Include(l => l.Seats)
            .FirstOrDefaultAsync(l => l.BusId == schedule.BusId, cancellationToken);
        if (layout is null)
            return Result.Failure<List<SeatAvailabilityDto>>(Error.NotFound("This bus has no seat layout configured."));

        var soldSeatIds = await _db.Tickets
            .Where(t => t.ScheduleId == request.ScheduleId && t.TravelDate == request.TravelDate && t.Status == TicketStatus.Sold)
            .Select(t => t.SeatId)
            .ToListAsync(cancellationToken);

        var soldSet = soldSeatIds.ToHashSet();

        var result = layout.Seats
            .OrderBy(s => s.RowLabel).ThenBy(s => s.ColumnNumber)
            .Select(s => new SeatAvailabilityDto(s.Id, s.SeatNumber, s.RowLabel, s.ColumnNumber, s.Class, s.IsActive, soldSet.Contains(s.Id)))
            .ToList();

        return Result.Success(result);
    }
}
