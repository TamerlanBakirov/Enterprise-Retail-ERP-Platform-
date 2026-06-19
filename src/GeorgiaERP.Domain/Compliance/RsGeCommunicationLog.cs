using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance;

public enum CommunicationDirection
{
    Request,
    Response
}

public class RsGeCommunicationLog : BaseEntity
{
    public Guid FiscalDocumentId { get; private set; }
    public string Operation { get; private set; } = default!;
    public CommunicationDirection Direction { get; private set; }
    public string Endpoint { get; private set; } = default!;
    public string? RequestPayload { get; private set; }
    public string? ResponsePayload { get; private set; }
    public int? HttpStatus { get; private set; }
    public int? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public FiscalDocument FiscalDocument { get; private set; } = default!;

    private RsGeCommunicationLog() { }

    public static RsGeCommunicationLog Create(
        Guid fiscalDocumentId,
        string operation,
        CommunicationDirection direction,
        string endpoint)
    {
        return new RsGeCommunicationLog
        {
            FiscalDocumentId = fiscalDocumentId,
            Operation = operation,
            Direction = direction,
            Endpoint = endpoint,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetRequest(string? requestPayload, Guid correlationId)
    {
        RequestPayload = requestPayload;
        CorrelationId = correlationId;
    }

    public void SetResponse(string? responsePayload, int? httpStatus, int? durationMs, string? errorMessage)
    {
        ResponsePayload = responsePayload;
        HttpStatus = httpStatus;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
    }
}
