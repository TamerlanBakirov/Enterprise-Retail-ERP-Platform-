using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Commands;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    Guid WarehouseId,
    Guid CreatedBy,
    DateTimeOffset? ExpectedDate,
    string? Notes,
    List<PoLineInput> Lines) : IRequest<Result<PurchaseOrderCreatedResponse>>;

public record PoLineInput(Guid ProductId, decimal Quantity, decimal UnitPrice, Guid? VariantId = null);

public record PurchaseOrderCreatedResponse(Guid Id, string PoNumber, decimal Total);

public class CreatePurchaseOrderCommandHandler
    : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderCreatedResponse>>
{
    private readonly IAppDbContext _dbContext;
    private const decimal VatRate = 0.18m;

    public CreatePurchaseOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PurchaseOrderCreatedResponse>> Handle(
        CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.IsActive, ct);
        if (!supplierExists) return Result.Failure<PurchaseOrderCreatedResponse>("Supplier not found or inactive.");

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists) return Result.Failure<PurchaseOrderCreatedResponse>("Warehouse not found.");

        var poNumber = $"PO-{DateTimeOffset.UtcNow:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
        var order = PurchaseOrder.Create(poNumber, request.SupplierId, request.WarehouseId, request.CreatedBy);

        if (request.ExpectedDate.HasValue) order.SetExpectedDate(request.ExpectedDate);
        if (request.Notes is not null) order.SetNotes(request.Notes);

        decimal subtotal = 0, vatTotal = 0;
        int lineNum = 1;

        foreach (var input in request.Lines)
        {
            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == input.ProductId && p.IsActive, ct);
            if (!productExists) return Result.Failure<PurchaseOrderCreatedResponse>($"Product {input.ProductId} not found.");

            var line = PurchaseOrderLine.Create(order.Id, lineNum++, input.ProductId, input.Quantity, input.UnitPrice, input.VariantId);

            var lineSubtotal = input.Quantity * input.UnitPrice;
            var lineVat = Math.Round(lineSubtotal * VatRate, 2);

            line.SetVat(lineVat);
            line.SetLineTotal(lineSubtotal + lineVat);

            subtotal += lineSubtotal;
            vatTotal += lineVat;

            order.Lines.Add(line);
        }

        order.SetTotals(subtotal, vatTotal, subtotal + vatTotal);

        _dbContext.PurchaseOrders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new PurchaseOrderCreatedResponse(order.Id, poNumber, order.Total));
    }
}
