using Asp.Versioning;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    public ReportsController(ISender sender) => _sender = sender;

    /// <summary>Daily revenue summary for a date range, optionally filtered by route.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(List<RevenueReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RevenueReportDto>>> GetRevenue([FromQuery] RevenueReportRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetRevenueReportQuery(request), cancellationToken));

    /// <summary>Per-trip occupancy breakdown for a date range, optionally filtered by route.</summary>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(List<OccupancyReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OccupancyReportDto>>> GetOccupancy([FromQuery] GetOccupancyReportQuery request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(request, cancellationToken));

    /// <summary>Top routes by ticket volume and revenue.</summary>
    [HttpGet("top-routes")]
    [ProducesResponseType(typeof(List<TopRouteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TopRouteDto>>> GetTopRoutes([FromQuery] GetTopRoutesQuery request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(request, cancellationToken));
}
