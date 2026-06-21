using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Warehouse;

public enum LocationType
{
    Zone,
    Aisle,
    Rack,
    Shelf,
    Bin
}

public class WarehouseLocation : BaseEntity
{
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public LocationType LocationType { get; private set; }
    public Guid? ParentLocationId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public int? MaxCapacity { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public WarehouseLocation? ParentLocation { get; private set; }
    public ICollection<WarehouseLocation> ChildLocations { get; private set; } = new List<WarehouseLocation>();

    private WarehouseLocation() { }

    public static WarehouseLocation Create(
        Guid warehouseId, string code, string name, LocationType locationType,
        Guid? parentLocationId = null, string? nameKa = null)
    {
        return new WarehouseLocation
        {
            WarehouseId = warehouseId,
            Code = code,
            Name = name,
            NameKa = nameKa,
            LocationType = locationType,
            ParentLocationId = parentLocationId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? nameKa, int sortOrder, int? maxCapacity, string? notes)
    {
        Name = name;
        NameKa = nameKa;
        SortOrder = sortOrder;
        MaxCapacity = maxCapacity;
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
