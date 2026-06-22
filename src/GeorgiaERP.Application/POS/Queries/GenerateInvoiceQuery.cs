using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Queries;

public record GenerateInvoiceQuery(Guid TransactionId) : IRequest<Result<byte[]>>;

public class GenerateInvoiceQueryHandler : IRequestHandler<GenerateInvoiceQuery, Result<byte[]>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPdfGenerationService _pdfService;

    public GenerateInvoiceQueryHandler(IAppDbContext dbContext, IPdfGenerationService pdfService)
    {
        _dbContext = dbContext;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GenerateInvoiceQuery request, CancellationToken cancellationToken)
    {
        var tx = await _dbContext.PosTransactions
            .Include(t => t.Lines)
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (tx is null)
            return Result.NotFound<byte[]>("PosTransaction", request.TransactionId);

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == tx.StoreId, cancellationToken);

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(cancellationToken);

        var customer = tx.CustomerId.HasValue
            ? await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == tx.CustomerId.Value, cancellationToken)
            : null;

        var invoiceData = new InvoiceData
        {
            InvoiceNumber = tx.TransactionNumber,
            Date = tx.CreatedAt,
            SellerName = company?.Name ?? "Company",
            SellerNameKa = company?.NameKa,
            SellerTin = company?.Tin ?? "",
            SellerAddress = company?.ActualAddress ?? company?.LegalAddress,
            SellerPhone = company?.Phone,
            SellerEmail = company?.Email,
            SellerIsVatPayer = company?.IsVatPayer ?? false,
            BuyerName = customer is not null
                ? customer.CompanyName ?? $"{customer.FirstName} {customer.LastName}"
                : null,
            BuyerTin = customer?.Tin,
            BuyerAddress = null,
            Lines = tx.Lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceLineData(
                l.LineNumber,
                l.ProductName,
                "pcs",
                l.Quantity,
                l.UnitPrice,
                l.VatAmount,
                l.LineTotal)).ToList(),
            Subtotal = tx.Subtotal,
            VatTotal = tx.VatTotal,
            Total = tx.Total,
            Currency = "GEL"
        };

        var pdfBytes = _pdfService.GenerateInvoice(invoiceData);
        return Result.Success(pdfBytes);
    }
}
