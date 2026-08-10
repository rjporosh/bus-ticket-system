using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PermissionAuthorizationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            context.Fail();
            return;
        }

        try
        {
            var roleId = await _db.Users
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => u.RoleId)
                .FirstOrDefaultAsync();

            if (roleId == Guid.Empty)
            {
                context.Fail();
                return;
            }

            var hasPermission = await _db.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.Permission == requirement.Permission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        catch (Exception)
        {
            context.Fail();
        }
    }
}
