using BusTicketing.Domain.Entities;

namespace BusTicketing.Application.Common.Interfaces;

/// <summary>Read-only accessor for the caller's identity, populated from the validated JWT by an ASP.NET Core middleware/service.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

/// <summary>Testable wrapper over DateTimeOffset.UtcNow.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Hashing abstraction so Application code never touches a concrete crypto library directly.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public record JwtTokenResult(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc);

/// <summary>Issues and validates JWT access tokens. Refresh-token persistence is handled separately via the User aggregate.</summary>
public interface IJwtTokenService
{
    JwtTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

/// <summary>Structured audit trail writer, independent of Serilog request logging (that captures HTTP traffic; this captures business-meaningful actions).</summary>
public interface IAuditLogService
{
    Task LogAsync(string action, string entityName, string entityId, string? details = null, CancellationToken cancellationToken = default);
}
