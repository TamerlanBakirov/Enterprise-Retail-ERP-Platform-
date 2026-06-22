using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Queries;

public record GenerateReceiptQuery(Guid TransactionId) : IRequest<Result<byte[]>>;

public class GenerateReceiptQueryHandler : IRequestHandler<GenerateReceiptQuery, Result<byte[]>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPdfGenerationService _pdfService;

    public GenerateReceiptQueryHandler(IAppDbContext dbContext, IPdfGenerationService pdfService)
    {
        _dbContext = dbContext;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GenerateReceiptQuery request, CancellationToken cancellationToken)
    {
        var tx = await _dbContext.PosTransactions
            .Include(t => t.Lines)
            .Include(t => t.Payments)
            .Include(t => t.Session)
                .ThenInclude(s => s.Terminal)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (tx is null)
            return Result.NotFound<byte[]>("PosTransaction", request.TransactionId);

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == tx.StoreId, cancellationToken);

        var cashier = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == tx.Session.CashierId, cancellationToken);

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(cancellationToken);

        var customer = tx.CustomerId.HasValue
            ? await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == tx.CustomerId.Value, cancellationToken)
            : null;

        var receiptData = new ReceiptData
        {
            TransactionNumber = tx.TransactionNumber,
            Date = tx.CreatedAt,
            StoreName = store?.Name ?? "Store",
            StoreAddress = store?.Address is not null
                ? $"{store.Address}{(store.City is not null ? $", {store.City}" : "")}"
                : null,
            CompanyName = company?.Name,
            CompanyTin = company?.Tin,
            CashierName = cashier is not null ? $"{cashier.FirstName} {cashier.LastName}" : null,
            TerminalId = tx.Session.Terminal.Code,
            Lines = tx.Lines.OrderBy(l => l.LineNumber).Select(l => new ReceiptLineData(
                l.ProductName,
                l.Quantity,
                "pcs",
                l.UnitPrice,
                l.DiscountAmount,
                l.LineTotal)).ToList(),
            Subtotal = tx.Subtotal,
            DiscountTotal = tx.DiscountTotal,
            VatTotal = tx.VatTotal,
            Total = tx.Total,
            Payments = tx.Payments.Select(p => new ReceiptPaymentData(
                p.PaymentMethod.ToString(),
                p.Amount,
                p.ChangeAmount)).ToList(),
            FiscalReceiptId = tx.FiscalReceiptId,
            CustomerName = customer is not null ? $"{customer.FirstName} {customer.LastName}" : null
        };

        var pdfBytes = _pdfService.GenerateReceipt(receiptData);
        return Result.Success(pdfBytes);
    }
}
