using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Identity;

public class Permission : BaseEntity
{
    public string Module { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string Resource { get; private set; } = default!;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Permission() { }

    public static Permission Create(string module, string action, string resource)
    {
        return new Permission
        {
            Module = module,
            Action = action,
            Resource = resource
        };
    }
}
