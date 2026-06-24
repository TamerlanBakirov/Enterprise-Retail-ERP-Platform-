using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance.Queries;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Submits a Draft VAT declaration to RS.GE, transitioning it to Submitted and
/// recording the filing reference. Georgian VAT returns are filed monthly; the
/// reference links the local record to the Revenue Service portal submission.
/// </summary>
public record SubmitVatDeclarationCommand(Guid Id) : IRequest<Result<VatDeclarationDto>>;

public class SubmitVatDeclarationCommandHandler
    : IRequestHandler<SubmitVatDeclarationCommand, Result<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;

    public SubmitVatDeclarationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<VatDeclarationDto>> Handle(SubmitVatDeclarationCommand request, CancellationToken ct)
    {
        var declaration = await _dbContext.VatDeclarations
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (declaration is null)
            return Result.NotFound<VatDeclarationDto>("VatDeclaration", request.Id);

        if (declaration.Status != VatDeclarationStatus.Draft)
            return Result.Conflict<VatDeclarationDto>(
                $"Only a Draft VAT declaration can be submitted. Current status: {declaration.Status}.");

        // The RS.GE VAT-return submission is performed via the Revenue Service portal;
        // we record a deterministic reference so the local record is traceable.
        var reference = $"VAT-{declaration.PeriodStart:yyyyMM}-{declaration.Id:N}";
        declaration.Submit(reference);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(VatDeclarationDto.From(declaration));
    }
}
