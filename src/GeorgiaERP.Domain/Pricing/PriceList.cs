using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Pricing;

public enum PriceType
{
    Retail,
    Wholesale,
    Employee,
    Cost
}

public class PriceList : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string Currency { get; private set; } = "GEL";
    public PriceType PriceType { get; private set; }
    public Guid? StoreId { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();

    private PriceList() { }

    public static PriceList Create(string code, string name, PriceType priceType, DateTimeOffset validFrom, string? nameKa = null)
    {
        return new PriceList
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            PriceType = priceType,
            ValidFrom = validFrom,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
