using Asp.Versioning;
using BusTicketing.Api.Middleware;
using BusTicketing.Application.Common.Models;
using BusTicketing.Application.Features.Users;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = "Permission:UserManage")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;
    public UsersController(ISender sender) => _sender = sender;

    /// <summary>Lists users with optional search, role and active-status filters, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<UserDto>>> GetUsers(
        [FromQuery] string? search, [FromQuery] Guid? roleId, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetUsersQuery(search, roleId, isActive, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a single user by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.ToApiResult();
    }

    /// <summary>Creates a new user (Admin or BoothStaff).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(user => Microsoft.AspNetCore.Http.Results.Created($"/api/v1/users/{user.Id}", user));
    }

    /// <summary>Updates a user's profile, contact info and role.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateUserCommand(id, request.FullName, request.PhoneNumber, request.BoothName, request.RoleId),
            cancellationToken);
        return result.ToApiResult();
    }

    /// <summary>Activates or deactivates a user's ability to log in.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetActiveStatus(Guid id, [FromBody] SetActiveStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetUserActiveStatusCommand(id, request.IsActive), cancellationToken);
        return result.ToApiResult();
    }
}

public record UpdateUserRequest(string FullName, string? PhoneNumber, string? BoothName, Guid RoleId);
public record SetActiveStatusRequest(bool IsActive);
