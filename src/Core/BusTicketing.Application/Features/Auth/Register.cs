using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusTicketing.Application.Features.Auth;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber) : IRequest<Result<AuthResponseDto>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;
    private readonly JwtSettings _jwtSettings;

    public RegisterCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTime,
        IAuditLogService auditLog,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTime = dateTime;
        _auditLog = auditLog;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate username or email
        var usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username.Trim(), cancellationToken);
        if (usernameExists)
            return Result.Failure<AuthResponseDto>(Error.Conflict("Username is already taken."));

        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (emailExists)
            return Result.Failure<AuthResponseDto>(Error.Conflict("Email is already registered."));

        // Get or create the Customer role
        var customerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == SystemRoles.Customer, cancellationToken);
        if (customerRole is null)
        {
            customerRole = Role.Create(SystemRoles.Customer, "Self-service customer account.", isSystemRole: true);
            _db.Roles.Add(customerRole);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another concurrent registration created the same system role
                // between our check and this insert (unique index on Role.Name).
                // Detach our speculative insert and use the row that won the race
                // instead of letting the unique-constraint violation bubble up.
                _db.Roles.Remove(customerRole);
                customerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == SystemRoles.Customer, cancellationToken)
                    ?? throw new InvalidOperationException("Failed to create or retrieve the Customer role.");
            }
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(
            request.Username,
            request.Email,
            passwordHash,
            request.FullName,
            customerRole.Id,
            phoneNumber: request.PhoneNumber);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        // Auto-login after registration
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = _dateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiresAt);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("Register", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken.AccessToken,
            accessToken.AccessTokenExpiresAtUtc,
            refreshTokenValue,
            refreshTokenExpiresAt,
            new UserSummaryDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.BoothName)));
    }
}