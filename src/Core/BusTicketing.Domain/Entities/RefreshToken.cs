using BusTicketing.Domain.Common;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// An opaque, single-use refresh token. Rotation is enforced: redeeming a token
/// revokes it and records the token that replaced it, so a reused/stolen token
/// is detectable and the whole chain can be revoked.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { } // EF Core

    public static RefreshToken Create(Guid userId, string token, DateTimeOffset expiresAtUtc)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public void Revoke(string? replacedByToken = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        ReplacedByToken = replacedByToken;
    }
}
