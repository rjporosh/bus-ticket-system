using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusTicketing.Application.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<Result<AuthResponseDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _auditLog;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(
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

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        // Deliberately identical failure message for "not found" and "wrong password"
        // to avoid leaking which usernames exist.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid username or password."));

        if (!user.IsActive)
            return Result.Failure<AuthResponseDto>(Error.Forbidden("This account has been deactivated."));

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = _dateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiresAt);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("Login", nameof(Domain.Entities.User), user.Id.ToString(), cancellationToken: cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken.AccessToken,
            accessToken.AccessTokenExpiresAtUtc,
            refreshTokenValue,
            refreshTokenExpiresAt,
            new UserSummaryDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.BoothName)));
    }
}
