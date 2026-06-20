using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Queues a follow-up RS.GE operation (confirm or close) for an already-submitted
/// waybill. The fiscal document must already carry an RS.GE id.
/// </summary>
public record EnqueueWaybillOperationCommand(
    Guid FiscalDocumentId,
    RsGeOperation Operation) : IRequest<Result>;

public class EnqueueWaybillOperationCommandHandler : IRequestHandler<EnqueueWaybillOperationCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly IRsGeQueuePublisher _publisher;

    public EnqueueWaybillOperationCommandHandler(IAppDbContext dbContext, IRsGeQueuePublisher publisher)
    {
        _dbContext = dbContext;
        _publisher = publisher;
    }

    public async Task<Result> Handle(EnqueueWaybillOperationCommand request, CancellationToken cancellationToken)
    {
        if (request.Operation is RsGeOperation.SubmitWaybill or RsGeOperation.SubmitInvoice)
            return Result.Failure("Use the create endpoints for initial submission.");

        var document = await _dbContext.FiscalDocuments
            .FirstOrDefaultAsync(d => d.Id == request.FiscalDocumentId, cancellationToken);

        if (document is null)
            return Result.Failure("Fiscal document not found.");

        if (string.IsNullOrEmpty(document.RsGeId))
            return Result.Failure("Document has not been submitted to RS.GE yet.");

        await _publisher.PublishAsync(
            new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = request.Operation },
            cancellationToken);

        return Result.Success();
    }
}
