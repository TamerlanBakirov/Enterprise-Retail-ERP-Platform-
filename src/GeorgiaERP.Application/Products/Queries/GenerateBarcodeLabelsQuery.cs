using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Queries;

public record GenerateBarcodeLabelsQuery(
    List<Guid> ProductIds,
    BarcodeLabelSize Size,
    bool IncludePrice) : IRequest<Result<byte[]>>;

public class GenerateBarcodeLabelsQueryHandler : IRequestHandler<GenerateBarcodeLabelsQuery, Result<byte[]>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPdfGenerationService _pdfService;

    public GenerateBarcodeLabelsQueryHandler(IAppDbContext dbContext, IPdfGenerationService pdfService)
    {
        _dbContext = dbContext;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GenerateBarcodeLabelsQuery request, CancellationToken cancellationToken)
    {
        if (request.ProductIds.Count == 0)
            return Result.Failure<byte[]>("At least one product ID is required.", "VALIDATION_ERROR");

        var products = await _dbContext.Products
            .Include(p => p.Barcodes)
            .Where(p => request.ProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
            return Result.Failure<byte[]>("No products found for the given IDs.", "NOT_FOUND");

        Dictionary<Guid, (decimal Price, string Currency)> priceMap = [];

        if (request.IncludePrice)
        {
            var now = DateTimeOffset.UtcNow;
            var priceItems = await _dbContext.PriceListItems
                .Include(pi => pi.PriceList)
                .Where(pi => request.ProductIds.Contains(pi.ProductId)
                    && pi.VariantId == null
                    && pi.MinQty <= 1
                    && pi.PriceList.IsActive
                    && pi.PriceList.PriceType == PriceType.Retail
                    && pi.PriceList.ValidFrom <= now
                    && (pi.PriceList.ValidTo == null || pi.PriceList.ValidTo > now))
                .OrderByDescending(pi => pi.PriceList.Priority)
                .ToListAsync(cancellationToken);

            foreach (var item in priceItems)
            {
                priceMap.TryAdd(item.ProductId, (item.Price, item.PriceList.Currency));
            }
        }

        var labels = new List<BarcodeLabelData>();

        foreach (var productId in request.ProductIds)
        {
            var product = products.FirstOrDefault(p => p.Id == productId);
            if (product is null)
                continue;

            var barcode = product.Barcodes
                .OrderByDescending(b => b.IsPrimary)
                .FirstOrDefault();

            if (barcode is null)
                continue;

            priceMap.TryGetValue(product.Id, out var priceInfo);

            labels.Add(new BarcodeLabelData(
                Barcode: barcode.Barcode,
                BarcodeType: barcode.BarcodeType.ToString(),
                ProductName: product.Name,
                Sku: product.Sku,
                Price: request.IncludePrice ? priceInfo.Price : null,
                Currency: request.IncludePrice ? priceInfo.Currency : null));
        }

        if (labels.Count == 0)
            return Result.Failure<byte[]>("None of the selected products have barcodes.", "VALIDATION_ERROR");

        var pdfBytes = _pdfService.GenerateBarcodeLabels(labels, request.Size);
        return Result.Success(pdfBytes);
    }
}
