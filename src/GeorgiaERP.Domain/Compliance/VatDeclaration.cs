using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance;

public enum VatDeclarationStatus
{
    Draft,
    Submitted,
    Accepted,
    Rejected
}

/// <summary>
/// A monthly VAT return for a tax period. Output VAT (collected on sales) less
/// input VAT (paid on purchases) yields the net VAT payable to RS.GE. The
/// declaration is computed as a Draft, then submitted, and finally marked
/// Accepted or Rejected based on the Revenue Service response.
/// </summary>
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
    public Guid CreatedBy { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VatDeclaration() { }

    public static VatDeclaration Create(DateTimeOffset periodStart, DateTimeOffset periodEnd, Guid createdBy)
    {
        if (periodEnd <= periodStart)
            throw new InvalidOperationException("VAT declaration period end must be after the period start.");

        return new VatDeclaration
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = VatDeclarationStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Sets the computed VAT totals and derives the net VAT (output - input).
    /// Only a Draft declaration may be (re)computed; a submitted return is immutable.
    /// </summary>
    public void SetTotals(decimal totalOutputVat, decimal totalInputVat)
    {
        if (Status != VatDeclarationStatus.Draft)
            throw new InvalidOperationException(
                $"VAT declaration totals can only be set while in Draft. Current status: {Status}.");
        if (totalOutputVat < 0) throw new InvalidOperationException("Output VAT cannot be negative.");
        if (totalInputVat < 0) throw new InvalidOperationException("Input VAT cannot be negative.");

        TotalOutputVat = totalOutputVat;
        TotalInputVat = totalInputVat;
        NetVat = totalOutputVat - totalInputVat;
    }

    /// <summary>Marks the declaration as filed with RS.GE, recording the returned reference and acting user.</summary>
    public void Submit(string rsGeReference, Guid submittedBy)
    {
        if (Status != VatDeclarationStatus.Draft)
            throw new InvalidOperationException(
                $"Only a Draft VAT declaration can be submitted. Current status: {Status}.");
        if (string.IsNullOrWhiteSpace(rsGeReference))
            throw new InvalidOperationException("An RS.GE reference is required to submit a VAT declaration.");

        Status = VatDeclarationStatus.Submitted;
        RsGeReference = rsGeReference;
        SubmittedAt = DateTimeOffset.UtcNow;
        SubmittedBy = submittedBy;
    }

    /// <summary>RS.GE accepted the filed return (terminal success).</summary>
    public void MarkAccepted()
    {
        if (Status != VatDeclarationStatus.Submitted)
            throw new InvalidOperationException(
                $"Only a Submitted VAT declaration can be accepted. Current status: {Status}.");
        Status = VatDeclarationStatus.Accepted;
    }

    /// <summary>RS.GE rejected the filed return (requires correction and re-filing).</summary>
    public void MarkRejected()
    {
        if (Status != VatDeclarationStatus.Submitted)
            throw new InvalidOperationException(
                $"Only a Submitted VAT declaration can be rejected. Current status: {Status}.");
        Status = VatDeclarationStatus.Rejected;
    }

    /// <summary>
    /// Reopens a Rejected declaration for correction so the same period can be
    /// re-computed and re-filed. The unique period constraint means a fresh row
    /// cannot be created, so the rejected record is reused. Clears the prior
    /// submission so a corrected return starts clean.
    /// </summary>
    public void RevertToDraft()
    {
        if (Status != VatDeclarationStatus.Rejected)
            throw new InvalidOperationException(
                $"Only a Rejected VAT declaration can be reverted to Draft. Current status: {Status}.");
        Status = VatDeclarationStatus.Draft;
        SubmittedAt = null;
        SubmittedBy = null;
        RsGeReference = null;
    }
}
