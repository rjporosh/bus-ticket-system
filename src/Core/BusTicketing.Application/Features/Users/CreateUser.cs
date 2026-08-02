using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Users;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    Guid RoleId,
    string? PhoneNumber,
    string? BoothName) : IRequest<Result<UserDto>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-zA-Z0-9_.]+$").WithMessage("Username may only contain letters, digits, underscores and dots.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;

    public CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher, IAuditLogService auditLog)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<UserDto>(Error.NotFound("The specified role does not exist."));

        var usernameTaken = await _db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (usernameTaken)
            return Result.Failure<UserDto>(Error.Conflict($"Username \"{request.Username}\" is already taken."));

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower(), cancellationToken);
        if (emailTaken)
            return Result.Failure<UserDto>(Error.Conflict($"Email \"{request.Email}\" is already registered."));

        var user = User.Create(
            request.Username,
            request.Email,
            _passwordHasher.Hash(request.Password),
            request.FullName,
            request.RoleId,
            request.PhoneNumber,
            request.BoothName);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync("Create", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);

        return Result.Success(new UserDto(
            user.Id, user.Username, user.Email, user.FullName, user.PhoneNumber,
            user.BoothName, user.IsActive, role.Id, role.Name, user.CreatedAtUtc));
    }
}
