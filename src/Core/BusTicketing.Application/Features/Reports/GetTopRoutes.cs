using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BusTicketing.Application.Features.Reports;

public record GetTopRoutesQuery(int TopCount = 10, DateOnly? FromDate = null, DateOnly? ToDate = null) : IRequest<Result<List<TopRouteDto>>>;

public class GetTopRoutesQueryHandler : IRequestHandler<GetTopRoutesQuery, Result<List<TopRouteDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetTopRoutesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<TopRouteDto>>> Handle(GetTopRoutesQuery request, CancellationToken cancellationToken)
    {
        var from = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));
        var to = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var topRoutes = await _db.Tickets
            .Include(t => t.Schedule).ThenInclude(s => s.Route)
            .Where(t => t.TravelDate >= from && t.TravelDate <= to && t.Status == TicketStatus.Sold)
            .GroupBy(t => new { t.Schedule.RouteId, t.Schedule.Route.Name })
            .Select(g => new TopRouteDto(
                g.Key.Name,
                g.Count(),
                g.Sum(t => t.FareAmount),
                g.Average(t => t.FareAmount)))
            .ToListAsync(cancellationToken);

        var ordered = System.Linq.Enumerable.OrderByDescending(topRoutes, r => r.TotalTicketsSold).Take(request.TopCount).ToList();

        return Result.Success(ordered);
    }
}
