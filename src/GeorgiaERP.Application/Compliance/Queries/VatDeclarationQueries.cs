using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Compliance.Queries;

public record VatDeclarationDto(
    Guid Id,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal TotalOutputVat,
    decimal TotalInputVat,
    decimal NetVat,
    string Status,
    DateTimeOffset? SubmittedAt,
    string? RsGeReference,
    Guid CreatedBy,
    Guid? SubmittedBy,
    DateTimeOffset CreatedAt)
{
    public static VatDeclarationDto From(VatDeclaration v) => new(
        v.Id,
        v.PeriodStart,
        v.PeriodEnd,
        v.TotalOutputVat,
        v.TotalInputVat,
        v.NetVat,
        v.Status.ToString(),
        v.SubmittedAt,
        v.RsGeReference,
        v.CreatedBy,
        v.SubmittedBy,
        v.CreatedAt);
}

public record GetVatDeclarationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<VatDeclarationDto>>;

public class GetVatDeclarationsQueryHandler
    : IRequestHandler<GetVatDeclarationsQuery, IReadOnlyList<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetVatDeclarationsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<VatDeclarationDto>> Handle(GetVatDeclarationsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return await _dbContext.VatDeclarations
            .AsNoTracking()
            .OrderByDescending(v => v.PeriodStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VatDeclarationDto(
                v.Id,
                v.PeriodStart,
                v.PeriodEnd,
                v.TotalOutputVat,
                v.TotalInputVat,
                v.NetVat,
                v.Status.ToString(),
                v.SubmittedAt,
                v.RsGeReference,
                v.CreatedBy,
                v.SubmittedBy,
                v.CreatedAt))
            .ToListAsync(ct);
    }
}

public record GetVatDeclarationByIdQuery(Guid Id) : IRequest<Result<VatDeclarationDto>>;

public class GetVatDeclarationByIdQueryHandler
    : IRequestHandler<GetVatDeclarationByIdQuery, Result<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetVatDeclarationByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<VatDeclarationDto>> Handle(GetVatDeclarationByIdQuery request, CancellationToken ct)
    {
        var declaration = await _dbContext.VatDeclarations
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        return declaration is null
            ? Result.NotFound<VatDeclarationDto>("VatDeclaration", request.Id)
            : Result.Success(VatDeclarationDto.From(declaration));
    }
}
