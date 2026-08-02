using System.Security.Cryptography;
using BusTicketing.Application.Common.Interfaces;

namespace BusTicketing.Infrastructure.Services;

/// <summary>
/// PBKDF2-HMACSHA256 password hashing (RFC 2898 / NIST SP 800-132), formatted as
/// "{iterations}.{saltBase64}.{hashBase64}" so the iteration count and salt travel
/// with the hash and can be upgraded later without invalidating existing hashes.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int Iterations = 210_000; // OWASP 2023+ recommended minimum for PBKDF2-HMACSHA256

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedKey = Convert.FromBase64String(parts[2]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
