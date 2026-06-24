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

        // The unique period constraint allows only one row per period. An existing
        // active (Draft/Submitted/Accepted) declaration blocks regeneration; a
        // Rejected one is reused — reverted to Draft and recomputed for re-filing.
        var existing = await _dbContext.VatDeclarations
            .FirstOrDefaultAsync(v => v.PeriodStart == periodStart, ct);
        if (existing is not null && existing.Status != VatDeclarationStatus.Rejected)
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

        // Input VAT: VAT on fully received purchases. PartiallyReceived POs are
        // excluded — claiming their full VatTotal would overstate the credit when
        // only part of the goods (and invoice) have arrived.
        var inputVat = await _dbContext.PurchaseOrders
            .Where(p => p.Status == PurchaseOrderStatus.Received &&
                        p.OrderDate >= periodStart && p.OrderDate < periodEnd)
            .SumAsync(p => (decimal?)p.VatTotal, ct) ?? 0m;

        VatDeclaration declaration;
        if (existing is not null)
        {
            // Reuse the rejected row for the period; recompute and re-open as Draft.
            existing.RevertToDraft();
            existing.SetTotals(outputVat, inputVat);
            declaration = existing;
        }
        else
        {
            declaration = VatDeclaration.Create(periodStart, periodEnd);
            declaration.SetTotals(outputVat, inputVat);
            _dbContext.VatDeclarations.Add(declaration);
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a race against a concurrent generate for the same period; the
            // unique period index rejected the insert. Surface as a clean conflict.
            return Result.Conflict<VatDeclarationDto>(
                $"A VAT declaration for {request.Year:D4}-{request.Month:D2} already exists.");
        }

        return Result.Success(VatDeclarationDto.From(declaration));
    }
}
