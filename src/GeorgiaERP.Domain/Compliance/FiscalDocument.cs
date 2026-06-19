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

    public void SetDocumentData(string json)
    {
        DocumentData = json;
        Touch();
    }

    public void SetSubmissionDeadline(DateTimeOffset deadline)
    {
        SubmissionDeadline = deadline;
        Touch();
    }

    /// <summary>Transitions the document into the queue, ready for asynchronous RS.GE submission.</summary>
    public void MarkQueued()
    {
        Status = FiscalDocumentStatus.Queued;
        Touch();
    }

    /// <summary>The request reached RS.GE and a server-side identifier was assigned.</summary>
    public void MarkSubmitted(string? rsGeId, string? documentNumber = null)
    {
        Status = FiscalDocumentStatus.Submitted;
        RsGeId = rsGeId;
        if (documentNumber is not null)
            DocumentNumber = documentNumber;
        SubmittedAt = DateTimeOffset.UtcNow;
        LastError = null;
        Touch();
    }

    /// <summary>RS.GE accepted and confirmed the document (terminal success).</summary>
    public void MarkConfirmed(string? rsGeStatus = null)
    {
        Status = FiscalDocumentStatus.Confirmed;
        RsGeStatus = rsGeStatus;
        ConfirmedAt = DateTimeOffset.UtcNow;
        LastError = null;
        Touch();
    }

    /// <summary>RS.GE rejected the document (terminal failure requiring manual correction).</summary>
    public void MarkRejected(string? error)
    {
        Status = FiscalDocumentStatus.Rejected;
        LastError = error;
        Touch();
    }

    /// <summary>A transient failure occurred; the document remains eligible for retry.</summary>
    public void MarkFailed(string? error)
    {
        Status = FiscalDocumentStatus.Failed;
        LastError = error;
        Touch();
    }

    public void IncrementRetry()
    {
        RetryCount++;
        Touch();
    }

    public bool HasExceededRetries(int maxRetries) => RetryCount >= maxRetries;

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
