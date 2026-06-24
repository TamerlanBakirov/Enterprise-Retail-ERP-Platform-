using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance.Queries;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Manually records the RS.GE outcome of a Submitted VAT declaration. RS.GE
/// responses can arrive out-of-band (e.g. the return was finalised on the
/// Revenue Service portal) rather than through the async worker, so an
/// accountant needs to mark a declaration Accepted or Rejected directly.
/// Only the declaration is transitioned; the async submission tracker, if any,
/// follows its own worker-driven lifecycle.
/// </summary>
public record ResolveVatDeclarationCommand(Guid Id, bool Accepted) : IRequest<Result<VatDeclarationDto>>;

public class ResolveVatDeclarationCommandHandler
    : IRequestHandler<ResolveVatDeclarationCommand, Result<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;

    public ResolveVatDeclarationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<VatDeclarationDto>> Handle(ResolveVatDeclarationCommand request, CancellationToken ct)
    {
        var declaration = await _dbContext.VatDeclarations
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (declaration is null)
            return Result.NotFound<VatDeclarationDto>("VatDeclaration", request.Id);

        if (declaration.Status != VatDeclarationStatus.Submitted)
            return Result.Conflict<VatDeclarationDto>(
                $"Only a Submitted VAT declaration can be resolved. Current status: {declaration.Status}.");

        if (request.Accepted)
            declaration.MarkAccepted();
        else
            declaration.MarkRejected();

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(VatDeclarationDto.From(declaration));
    }
}
