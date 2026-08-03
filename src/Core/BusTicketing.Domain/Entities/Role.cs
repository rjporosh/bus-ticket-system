using BusTicketing.Domain.Common;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>A named authorization role (e.g. Admin, BoothStaff) referenced by JWT role claims.</summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>True for the two seeded system roles, which cannot be renamed or deleted.</summary>
    public bool IsSystemRole { get; private set; }

    private Role() { } // EF Core

    public static Role Create(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name is required.");

        return new Role
        {
            Name = name.Trim(),
            Description = description,
            IsSystemRole = isSystemRole
        };
    }

    public void Update(string name, string? description)
    {
        if (IsSystemRole)
            throw new DomainException($"System role \"{Name}\" cannot be modified.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name is required.");

        Name = name.Trim();
        Description = description;
    }
}
