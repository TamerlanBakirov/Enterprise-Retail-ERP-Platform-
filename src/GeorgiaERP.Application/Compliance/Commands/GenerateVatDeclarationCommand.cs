using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance.Queries;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Generates a Draft VAT declaration for a calendar month. Output VAT is summed
/// from completed POS sales (less returns) and input VAT from received purchase
/// orders in the period. Only one declaration may exist per period.
/// </summary>
public record GenerateVatDeclarationCommand(int Year, int Month) : IRequest<Result<VatDeclarationDto>>;

public class GenerateVatDeclarationCommandHandler
    : IRequestHandler<GenerateVatDeclarationCommand, Result<VatDeclarationDto>>
{
    private readonly IAppDbContext _dbContext;

    public GenerateVatDeclarationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<VatDeclarationDto>> Handle(GenerateVatDeclarationCommand request, CancellationToken ct)
    {
        var periodStart = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);

        var exists = await _dbContext.VatDeclarations
            .AnyAsync(v => v.PeriodStart == periodStart, ct);
        if (exists)
            return Result.Conflict<VatDeclarationDto>(
                $"A VAT declaration for {request.Year:D4}-{request.Month:D2} already exists.");

        // Output VAT: VAT collected on completed sales, net of returns.
        var sales = await _dbContext.PosTransactions
            .Where(t => t.Status == PosTransactionStatus.Completed &&
                        t.TransactionType == PosTransactionType.Sale &&
                        t.CreatedAt >= periodStart && t.CreatedAt < periodEnd)
            .SumAsync(t => (decimal?)t.VatTotal, ct) ?? 0m;

        var returns = await _dbContext.PosTransactions
            .Where(t => t.Status == PosTransactionStatus.Completed &&
                        t.TransactionType == PosTransactionType.Return &&
                        t.CreatedAt >= periodStart && t.CreatedAt < periodEnd)
            .SumAsync(t => (decimal?)t.VatTotal, ct) ?? 0m;

        var outputVat = Math.Max(0m, sales - returns);

        // Input VAT: VAT paid on purchases that have been received in the period.
        var inputVat = await _dbContext.PurchaseOrders
            .Where(p => (p.Status == PurchaseOrderStatus.Received ||
                         p.Status == PurchaseOrderStatus.PartiallyReceived) &&
                        p.OrderDate >= periodStart && p.OrderDate < periodEnd)
            .SumAsync(p => (decimal?)p.VatTotal, ct) ?? 0m;

        var declaration = VatDeclaration.Create(periodStart, periodEnd);
        declaration.SetTotals(outputVat, inputVat);

        _dbContext.VatDeclarations.Add(declaration);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(VatDeclarationDto.From(declaration));
    }
}
