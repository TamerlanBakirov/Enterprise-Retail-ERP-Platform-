using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Identity;

public class Role : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() { }

    public static Role Create(string code, string name, string? nameKa = null, string? description = null, bool isSystem = false)
    {
        return new Role
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            Description = description,
            IsSystem = isSystem,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
