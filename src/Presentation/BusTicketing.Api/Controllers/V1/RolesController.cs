using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Features.Roles;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize(Roles = SystemRoles.Admin)]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly ISender _sender;
    public RolesController(ISender sender) => _sender = sender;

    /// <summary>Lists all roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetRolesQuery(), cancellationToken));

    /// <summary>Creates a custom role.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(role => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/roles/{role.Id}", role));
    }

    /// <summary>Updates a custom role's name/description. System roles (Admin, BoothStaff) cannot be modified.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateRoleCommand(id, request.Name, request.Description), cancellationToken);
        return result.ToApiResult();
    }
}

public record UpdateRoleRequest(string Name, string? Description);
