using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance.Queries;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Files a Draft VAT declaration with RS.GE. Marks the declaration Submitted,
/// creates a linked fiscal-document tracker, and enqueues the submission so the
/// Revenue Service round-trip happens asynchronously in the worker. The worker
/// transitions the declaration to Accepted or Rejected based on the response.
/// </summary>
public record SubmitVatDeclarationCommand(Guid Id) : IRequest<Result<VatDeclarationDto>>;

public class SubmitVatDeclarationCommandHandler
    : IRequestHandler<SubmitVatDeclarationCommand, Result<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IRsGeQueuePublisher _publisher;
    private readonly ILogger<SubmitVatDeclarationCommandHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SubmitVatDeclarationCommandHandler(
        IAppDbContext dbContext,
        IRsGeQueuePublisher publisher,
        ILogger<SubmitVatDeclarationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<VatDeclarationDto>> Handle(SubmitVatDeclarationCommand request, CancellationToken ct)
    {
        var declaration = await _dbContext.VatDeclarations
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (declaration is null)
            return Result.NotFound<VatDeclarationDto>("VatDeclaration", request.Id);

        if (declaration.Status != VatDeclarationStatus.Draft)
            return Result.Conflict<VatDeclarationDto>(
                $"Only a Draft VAT declaration can be submitted. Current status: {declaration.Status}.");

        var reference = $"VAT-{declaration.PeriodStart:yyyyMM}-{declaration.Id:N}";
        declaration.Submit(reference);

        // The fiscal-document tracker carries the RS.GE submission lifecycle and
        // links back to the declaration via ReferenceId, mirroring the waybill flow.
        var document = FiscalDocument.Create(
            FiscalDocumentType.VatDeclaration,
            internalRef: reference,
            referenceType: nameof(VatDeclaration),
            referenceId: declaration.Id);
        document.SetDocumentData(JsonSerializer.Serialize(new
        {
            declaration.PeriodStart,
            declaration.PeriodEnd,
            declaration.TotalOutputVat,
            declaration.TotalInputVat,
            declaration.NetVat
        }, JsonOptions));
        document.MarkQueued();

        _dbContext.FiscalDocuments.Add(document);
        await _dbContext.SaveChangesAsync(ct);

        // Persisted first, then queued. A publish failure is non-fatal: the document
        // is durably Queued and the recovery sweep re-enqueues it.
        try
        {
            await _publisher.PublishAsync(
                new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = RsGeOperation.SubmitVatDeclaration },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "VAT declaration {DeclarationId} persisted but could not be queued; recovery sweep will retry",
                declaration.Id);
        }

        return Result.Success(VatDeclarationDto.From(declaration));
    }
}
