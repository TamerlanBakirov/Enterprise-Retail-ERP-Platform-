namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// The unit of work passed through the RS.GE submission queue. Carries the
/// identity of the fiscal document to submit and the operation to perform,
/// keeping the message small and idempotent — all business data is rehydrated
/// from the database by the consumer so the queue never holds stale payloads.
/// </summary>
public record RsGeSubmissionMessage
{
    public Guid FiscalDocumentId { get; init; }
    public RsGeOperation Operation { get; init; }
    public int Attempt { get; init; } = 1;

    public RsGeSubmissionMessage NextAttempt() => this with { Attempt = Attempt + 1 };
}

public enum RsGeOperation
{
    /// <summary>save_waybill followed by send_waybill — the full create-and-activate flow.</summary>
    SubmitWaybill,
    ConfirmWaybill,
    CloseWaybill,
    SubmitInvoice,

    /// <summary>save_vat_declaration — files a monthly VAT return with RS.GE.</summary>
    SubmitVatDeclaration
}
