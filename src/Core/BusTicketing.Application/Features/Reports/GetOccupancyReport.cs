using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BusTicketing.Application.Features.Reports;

public record GetOccupancyReportQuery(DateOnly? FromDate = null, DateOnly? ToDate = null, Guid? RouteId = null) : IRequest<Result<List<OccupancyReportDto>>>;

public class GetOccupancyReportQueryHandler : IRequestHandler<GetOccupancyReportQuery, Result<List<OccupancyReportDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetOccupancyReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<OccupancyReportDto>>> Handle(GetOccupancyReportQuery request, CancellationToken cancellationToken)
    {
        var from = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));
        var to = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var schedulesQuery = _db.Schedules
            .Include(s => s.Bus)
            .ThenInclude(b => b.SeatLayout)
            .ThenInclude(l => l.Seats)
            .Include(s => s.Route)
            .Where(s => s.RunsOn(from) && s.RunsOn(to))
            .AsQueryable();

        if (request.RouteId.HasValue)
            schedulesQuery = schedulesQuery.Where(s => s.RouteId == request.RouteId.Value);

        var schedules = await schedulesQuery.ToListAsync(cancellationToken);

        var result = new List<OccupancyReportDto>();

        foreach (var schedule in schedules)
        {
            var travelDates = Enumerable.Range(0, (to.DayNumber - from.DayNumber + 1))
                .Select(offset => from.AddDays(offset))
                .Where(d => schedule.RunsOn(d))
                .ToList();

            foreach (var date in travelDates)
            {
                var totalSeats = schedule.Bus.SeatLayout.Seats.Count(s => s.IsActive);
                var soldSeats = await _db.Tickets
                    .CountAsync(t => t.ScheduleId == schedule.Id && t.TravelDate == date && t.Status == TicketStatus.Sold, cancellationToken);

                var revenue = await _db.Tickets
                    .Where(t => t.ScheduleId == schedule.Id && t.TravelDate == date && t.Status == TicketStatus.Sold)
                    .SumAsync(t => t.FareAmount, cancellationToken);

                result.Add(new OccupancyReportDto(
                    schedule.Bus.Number,
                    schedule.Route.Name,
                    date,
                    schedule.DepartureTime,
                    totalSeats,
                    soldSeats,
                    totalSeats > 0 ? Math.Round((decimal)soldSeats / totalSeats * 100, 2) : 0,
                    revenue));
            }
        }

        return Result.Success(result.OrderBy(r => r.TravelDate).ThenBy(r => r.BusNumber).ToList());
    }
}
