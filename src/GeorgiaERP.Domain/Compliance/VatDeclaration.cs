using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance;

public enum VatDeclarationStatus
{
    Draft,
    Submitted,
    Accepted,
    Rejected
}

public class VatDeclaration : BaseEntity
{
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public decimal TotalOutputVat { get; private set; }
    public decimal TotalInputVat { get; private set; }
    public decimal NetVat { get; private set; }
    public VatDeclarationStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? RsGeReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VatDeclaration() { }

    public static VatDeclaration Create(DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        return new VatDeclaration
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = VatDeclarationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
