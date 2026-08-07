using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusTicketing.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTime;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTime,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _dateTime = dateTime;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid refresh token."));

        var existingToken = user.RefreshTokens.First(rt => rt.Token == request.RefreshToken);

        if (existingToken.IsRevoked)
        {
            // Token reuse after revocation is a strong signal of theft: revoke the
            // entire active token chain for this user as a precaution.
            foreach (var token in user.RefreshTokens.Where(rt => rt.IsActive))
                token.Revoke();

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("This refresh token has already been used. All sessions have been revoked for safety."));
        }

        if (!existingToken.IsActive)
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Refresh token has expired."));

        if (!user.IsActive)
            return Result.Failure<AuthResponseDto>(Error.Forbidden("This account has been deactivated."));

        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenExpiresAt = _dateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        existingToken.Revoke(newRefreshTokenValue);

        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenValue, newRefreshTokenExpiresAt);
        _db.RefreshTokens.Add(newRefreshToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken.AccessToken,
            accessToken.AccessTokenExpiresAtUtc,
            newRefreshTokenValue,
            newRefreshTokenExpiresAt,
            new UserSummaryDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.BoothName)));
    }
}
