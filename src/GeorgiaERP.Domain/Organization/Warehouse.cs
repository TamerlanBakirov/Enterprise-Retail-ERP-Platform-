using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Organization;

public enum WarehouseType
{
    Central,
    Regional,
    Store
}

public class Warehouse : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public WarehouseType WarehouseType { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public Guid? LinkedStoreId { get; private set; }
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; } // jsonb
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public Store? LinkedStore { get; private set; }

    private Warehouse() { }

    public static Warehouse Create(string code, string name, WarehouseType warehouseType, string? nameKa = null)
    {
        return new Warehouse
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            WarehouseType = warehouseType,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
