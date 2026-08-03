namespace BusTicketing.Application.Features.Users;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? BoothName,
    bool IsActive,
    Guid RoleId,
    string RoleName,
    DateTimeOffset CreatedAtUtc);
