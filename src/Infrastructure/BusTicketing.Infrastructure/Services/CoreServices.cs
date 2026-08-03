using System.Security.Claims;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace BusTicketing.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public class AuditLogService : IAuditLogService
{
    private readonly Persistence.ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;

    public AuditLogService(Persistence.ApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task LogAsync(string action, string entityName, string entityId, string? details = null, CancellationToken cancellationToken = default)
    {
        var entry = AuditLog.Create(
            action, entityName, entityId, details,
            _currentUser.UserId, _currentUser.Username ?? "system", _dateTime.UtcNow);

        _db.AuditLogs.Add(entry);
        // Intentionally not calling SaveChangesAsync here: audit entries ride along with
        // the surrounding unit of work's SaveChanges so a failed business operation never
        // leaves an orphaned "success" audit trail.
        await Task.CompletedTask;
    }
}
