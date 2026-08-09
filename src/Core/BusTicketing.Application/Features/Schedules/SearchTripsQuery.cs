using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Schedules;

public record SearchTripsQuery(
    DateOnly TravelDate,
    Guid? OriginStationId = null,
    Guid? DestinationStationId = null,
    string? OriginStationName = null,
    string? DestinationStationName = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<TripDto>>;

public class SearchTripsQueryHandler : IRequestHandler<SearchTripsQuery, PaginatedList<TripDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchTripsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedList<TripDto>> Handle(SearchTripsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Schedules
            .Include(s => s.Bus)
            .Include(s => s.Route)
            .Where(s => s.Status != ScheduleStatus.Cancelled)
            .Where(s => s.EffectiveFrom <= request.TravelDate)
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= request.TravelDate)
            .AsQueryable();

        if (request.OriginStationId.HasValue)
            query = query.Where(s => s.Route.OriginStationId == request.OriginStationId.Value);

        if (request.DestinationStationId.HasValue)
            query = query.Where(s => s.Route.DestinationStationId == request.DestinationStationId.Value);

        if (!string.IsNullOrWhiteSpace(request.OriginStationName))
            query = query.Where(s => EF.Functions.Like(s.Route.Origin.Name, $"%{request.OriginStationName.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(request.DestinationStationName))
            query = query.Where(s => EF.Functions.Like(s.Route.Destination.Name, $"%{request.DestinationStationName.Trim()}%"));

        var candidates = await query.ToListAsync(cancellationToken);
        var todaysSchedules = candidates
            .Where(s => s.RunsOn(request.TravelDate))
            .OrderBy(s => s.DepartureTime)
            .ToList();

        var scheduleIds = todaysSchedules.Select(s => s.Id).ToHashSet();

        var soldCountByScheduleId = await _db.Tickets
            .Where(t => scheduleIds.Contains(t.ScheduleId) && t.TravelDate == request.TravelDate && t.Status == TicketStatus.Sold)
            .GroupBy(t => t.ScheduleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        var results = todaysSchedules
            .Select(s => new TripDto(
                s.Id, s.BusId, s.Bus.Number, s.Route.Name, s.DepartureTime, s.ArrivalTime, s.FareAmount,
                s.Bus.TotalSeats, s.Bus.TotalSeats - soldCountByScheduleId.GetValueOrDefault(s.Id, 0)))
            .ToList();

        var totalCount = results.Count;
        var items = results
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedList<TripDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
