using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Users;

public record UpdateUserCommand(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    string? BoothName,
    Guid RoleId) : IRequest<Result<UserDto>>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogService _auditLog;

    public UpdateUserCommandHandler(IApplicationDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
            return Result.Failure<UserDto>(Error.NotFound($"User {request.Id} was not found."));

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<UserDto>(Error.NotFound("The specified role does not exist."));

        user.UpdateProfile(request.FullName, request.PhoneNumber, request.BoothName);
        user.ChangeRole(request.RoleId);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<UserDto>(Error.Conflict("This user was modified by someone else. Please reload and try again."));
        }

        await _auditLog.LogAsync("Update", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);

        return Result.Success(new UserDto(
            user.Id, user.Username, user.Email, user.FullName, user.PhoneNumber,
            user.BoothName, user.IsActive, role.Id, role.Name, user.CreatedAtUtc));
    }
}

public record SetUserActiveStatusCommand(Guid Id, bool IsActive) : IRequest<Result>;

public class SetUserActiveStatusCommandHandler : IRequestHandler<SetUserActiveStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogService _auditLog;

    public SetUserActiveStatusCommandHandler(IApplicationDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<Result> Handle(SetUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
            return Result.Failure(Error.NotFound($"User {request.Id} was not found."));

        if (request.IsActive) user.Activate(); else user.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(request.IsActive ? "Activate" : "Deactivate", nameof(Domain.Entities.User), user.Id.ToString(), cancellationToken: cancellationToken);

        return Result.Success();
    }
}
