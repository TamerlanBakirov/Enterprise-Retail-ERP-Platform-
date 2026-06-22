using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Organization;

public enum StoreType
{
    Retail,
    Outlet,
    Franchise
}

public class Store : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public StoreType StoreType { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? Phone { get; private set; }
    public Guid? ManagerUserId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string Timezone { get; private set; } = "Asia/Tbilisi";
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; } // jsonb
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Store() { }

    public static Store Create(string code, string name, StoreType storeType, string? nameKa = null)
    {
        return new Store
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            StoreType = storeType,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? nameKa, string? address, string? city, string? region, string? phone)
    {
        Name = name;
        NameKa = nameKa;
        Address = address;
        City = city;
        Region = region;
        Phone = phone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetManager(Guid? managerUserId) { ManagerUserId = managerUserId; UpdatedAt = DateTimeOffset.UtcNow; }
    public void SetLocation(double? latitude, double? longitude) { Latitude = latitude; Longitude = longitude; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
