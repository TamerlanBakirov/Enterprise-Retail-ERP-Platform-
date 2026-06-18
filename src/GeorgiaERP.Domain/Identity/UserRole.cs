using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Identity;

public class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? StoreId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = default!;
    public Role Role { get; private set; } = default!;

    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId, Guid? storeId = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            StoreId = storeId
        };
    }
}
