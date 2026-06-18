using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Organization;

public class Company : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string Tin { get; private set; } = default!;
    public bool IsVatPayer { get; private set; }
    public DateTimeOffset? VatRegistrationDate { get; private set; }
    public string? LegalAddress { get; private set; }
    public string? ActualAddress { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Settings { get; private set; } // jsonb
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Company() { }

    public static Company Create(string code, string name, string tin, bool isVatPayer = false, string? nameKa = null)
    {
        return new Company
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            Tin = tin,
            IsVatPayer = isVatPayer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
