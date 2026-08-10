using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BusTicketing.Application.Features.Reports;

public record GetRevenueReportQuery(RevenueReportRequest Request) : IRequest<Result<List<RevenueReportDto>>>;

public class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, Result<List<RevenueReportDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetRevenueReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<RevenueReportDto>>> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var from = request.Request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));
        var to = request.Request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var query = _db.Tickets
            .Include(t => t.Schedule).ThenInclude(s => s.Route)
            .Where(t => t.TravelDate >= from && t.TravelDate <= to && t.Status == TicketStatus.Sold);

        if (request.Request.RouteId.HasValue)
            query = query.Where(t => t.Schedule.RouteId == request.Request.RouteId.Value);

        var data = await query
            .GroupBy(t => t.TravelDate)
            .Select(g => new RevenueReportDto(
                g.Key,
                g.Sum(t => t.FareAmount),
                g.Count(),
                0,
                0m))
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

        foreach (var item in data)
        {
            var totalSeatsOnDate = await _db.Schedules
                .Include(s => s.Bus)
                .ThenInclude(b => b.SeatLayout)
                .Where(s => s.RunsOn(item.Date))
                .SelectMany(s => s.Bus.SeatLayout.Seats)
                .CountAsync(s => s.IsActive, cancellationToken);

            item.TotalSeatsAvailable = totalSeatsOnDate;
            item.OccupancyRate = totalSeatsOnDate > 0 ? Math.Round((decimal)item.TotalTicketsSold / totalSeatsOnDate * 100, 2) : 0;
        }

        return Result.Success(data);
    }
}
