namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// Executes a single RS.GE submission end-to-end: rehydrates the fiscal document,
/// builds the SOAP request, calls the Revenue Service, records the communication,
/// and transitions document status. Lives behind an interface so the queue
/// consumer (Workers) depends only on the Application contract.
/// </summary>
public interface IRsGeSubmissionProcessor
{
    Task<RsGeSubmissionResult> ProcessAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a submission attempt. <see cref="Outcome"/> drives the consumer's
/// ack/nack/dead-letter decision.
/// </summary>
public record RsGeSubmissionResult(RsGeSubmissionOutcome Outcome, string? Detail = null)
{
    public static RsGeSubmissionResult Success(string? detail = null) => new(RsGeSubmissionOutcome.Succeeded, detail);
    public static RsGeSubmissionResult Transient(string? detail) => new(RsGeSubmissionOutcome.TransientFailure, detail);
    public static RsGeSubmissionResult Permanent(string? detail) => new(RsGeSubmissionOutcome.PermanentFailure, detail);
}

public enum RsGeSubmissionOutcome
{
    /// <summary>Submission accepted by RS.GE — acknowledge and remove from queue.</summary>
    Succeeded,

    /// <summary>Network/availability error — retry with backoff, dead-letter after max attempts.</summary>
    TransientFailure,

    /// <summary>Business/validation rejection — do not retry; route to dead-letter for manual review.</summary>
    PermanentFailure
}
