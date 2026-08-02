using BusTicketing.Domain.Common;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// A system user: an Admin or a booth staff member. Password hashing and token
/// issuance live in the Infrastructure layer; this entity only guards its own
/// invariants (must have a role, cannot be disabled twice, etc.).
/// </summary>
public class User : BaseEntity
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Optional booth affiliation, e.g. "Dhaka" or "Chittagong". Null for Admins.</summary>
    public string? BoothName { get; private set; }

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    public static User Create(
        string username,
        string email,
        string passwordHash,
        string fullName,
        Guid roleId,
        string? phoneNumber = null,
        string? boothName = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber,
            RoleId = roleId,
            BoothName = boothName,
            IsActive = true
        };
    }

    public void UpdateProfile(string fullName, string? phoneNumber, string? boothName)
    {
        FullName = string.IsNullOrWhiteSpace(fullName) ? FullName : fullName.Trim();
        PhoneNumber = phoneNumber;
        BoothName = boothName;
    }

    public void ChangeRole(Guid roleId) => RoleId = roleId;

    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash is required.");
        PasswordHash = newPasswordHash;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public RefreshToken IssueRefreshToken(string token, DateTimeOffset expiresAtUtc)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAtUtc);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }
}
