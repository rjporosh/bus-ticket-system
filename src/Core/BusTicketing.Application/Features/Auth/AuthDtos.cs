namespace BusTicketing.Application.Features.Auth;

public record AuthResponseDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserSummaryDto User);

public record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    string? BoothName);
