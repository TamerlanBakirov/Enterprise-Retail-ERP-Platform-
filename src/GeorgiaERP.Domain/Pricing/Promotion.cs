using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Pricing;

public enum PromotionType
{
    Percentage,
    Fixed,
    BuyOneGetOne,
    Bundle
}

public class Promotion : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public PromotionType PromotionType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public string? Conditions { get; private set; } // jsonb
    public List<Guid>? StoreIds { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int? MaxUses { get; private set; }
    public int CurrentUses { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Promotion() { }

    public static Promotion Create(
        string code,
        string name,
        PromotionType promotionType,
        DateTimeOffset validFrom,
        decimal? discountValue = null,
        string? nameKa = null)
    {
        return new Promotion
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            PromotionType = promotionType,
            DiscountValue = discountValue,
            ValidFrom = validFrom,
            IsActive = true,
            CurrentUses = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Stops the promotion (e.g. ended early) without deleting its history.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Re-enables a previously deactivated promotion.</summary>
    public void Activate() => IsActive = true;
}
