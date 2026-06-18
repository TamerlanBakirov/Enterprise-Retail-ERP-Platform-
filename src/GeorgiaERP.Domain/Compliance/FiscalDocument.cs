using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance;

public enum FiscalDocumentType
{
    Waybill,
    Invoice,
    VatDeclaration,
    FiscalReceipt
}

public enum FiscalDocumentStatus
{
    Pending,
    Queued,
    Submitted,
    Confirmed,
    Rejected,
    Failed,
    Cancelled
}

public class FiscalDocument : BaseEntity
{
    public FiscalDocumentType DocumentType { get; private set; }
    public string? DocumentNumber { get; private set; }
    public string? InternalRef { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public FiscalDocumentStatus Status { get; private set; }
    public string? RsGeId { get; private set; }
    public string? RsGeStatus { get; private set; }
    public DateTimeOffset? SubmissionDeadline { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public string? DocumentData { get; private set; } // jsonb
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<RsGeCommunicationLog> CommunicationLogs { get; private set; } = new List<RsGeCommunicationLog>();

    private FiscalDocument() { }

    public static FiscalDocument Create(FiscalDocumentType documentType, string? internalRef = null, string? referenceType = null, Guid? referenceId = null)
    {
        return new FiscalDocument
        {
            DocumentType = documentType,
            InternalRef = internalRef,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Status = FiscalDocumentStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
