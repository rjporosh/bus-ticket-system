using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Dashboard;

public record GetDashboardSummaryQuery(DateOnly Date) : IRequest<DashboardSummaryDto>;

/// <summary>
/// Resolves the same trip set as GetTripsForDateQuery (Schedules module), then overlays
/// actual sold-ticket counts and revenue — this is what makes the Dashboard "real" rather
/// than the Phase-1 placeholder that could only show trip listings, since Booking data
/// now exists to aggregate.
/// </summary>
public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _db;
    public GetDashboardSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var candidates = await _db.Schedules
            .Include(s => s.Bus)
            .Include(s => s.Route)
            .Where(s => s.Status != ScheduleStatus.Cancelled)
            .Where(s => s.EffectiveFrom <= request.Date)
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= request.Date)
            .ToListAsync(cancellationToken);

        var todaysSchedules = candidates.Where(s => s.RunsOn(request.Date)).ToList();
        var scheduleIds = todaysSchedules.Select(s => s.Id).ToHashSet();

        var soldTicketsToday = await _db.Tickets
            .Where(t => t.TravelDate == request.Date && t.Status == TicketStatus.Sold && scheduleIds.Contains(t.ScheduleId))
            .Select(t => new { t.ScheduleId, t.FareAmount })
            .ToListAsync(cancellationToken);

        var soldCountByScheduleId = soldTicketsToday
            .GroupBy(t => t.ScheduleId)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalSeats = todaysSchedules.Sum(s => s.Bus.TotalSeats);
        var soldSeats = soldTicketsToday.Count;
        var totalSales = soldTicketsToday.Sum(t => t.FareAmount);

        var busWiseSeatStatus = todaysSchedules
            .OrderBy(s => s.DepartureTime)
            .Select(s => new BusSeatStatusDto(
                s.Bus.Number, s.Route.Name, s.DepartureTime,
                s.Bus.TotalSeats - soldCountByScheduleId.GetValueOrDefault(s.Id, 0),
                s.Bus.TotalSeats))
            .ToList();

        var routeWiseSales = todaysSchedules
            .GroupBy(s => s.Route.Name)
            .Select(g => new RouteSalesDto(
                g.Key,
                g.Sum(s => soldCountByScheduleId.GetValueOrDefault(s.Id, 0)),
                g.Sum(s => s.Bus.TotalSeats - soldCountByScheduleId.GetValueOrDefault(s.Id, 0)),
                soldTicketsToday.Where(t => g.Select(s => s.Id).Contains(t.ScheduleId)).Sum(t => t.FareAmount)))
            .OrderBy(r => r.RouteName)
            .ToList();

        return new DashboardSummaryDto(
            request.Date, totalSeats, soldSeats, totalSeats - soldSeats, totalSales,
            routeWiseSales, busWiseSeatStatus);
    }
}
