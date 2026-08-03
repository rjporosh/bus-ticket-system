using Asp.Versioning;
using BusTicketing.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly ISender _sender;
    public DashboardController(ISender sender) => _sender = sender;

    /// <summary>Sold/available seat counts, revenue, and route/bus breakdowns for a given date.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] DateOnly date, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetDashboardSummaryQuery(date), cancellationToken));
}
