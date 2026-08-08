using BusTicketing.Domain.Common;
using BusTicketing.Domain.Enums;

namespace BusTicketing.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public Permission Permission { get; private set; }

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Permission permission)
    {
        return new RolePermission
        {
            RoleId = roleId,
            Permission = permission,
        };
    }
}
