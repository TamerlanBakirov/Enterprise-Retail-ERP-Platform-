using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Procurement;

public class Supplier : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string? Tin { get; private set; }
    public bool IsVatPayer { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? PaymentTerms { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public int? Rating { get; private set; }
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; } // jsonb
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<PurchaseOrder> PurchaseOrders { get; private set; } = new List<PurchaseOrder>();

    private Supplier() { }

    public static Supplier Create(string code, string name, string? nameKa = null, string? tin = null)
    {
        return new Supplier
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            Tin = tin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
