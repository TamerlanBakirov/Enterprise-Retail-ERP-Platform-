using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using GeorgiaERP.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Commands;

public record SetPriceCommand(
    Guid PriceListId,
    Guid ProductId,
    decimal Price,
    decimal MinQty,
    Guid? VariantId) : IRequest<Result<PriceListItemDto>>;

public class SetPriceCommandHandler : IRequestHandler<SetPriceCommand, Result<PriceListItemDto>>
{
    private readonly IAppDbContext _db;

    public SetPriceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PriceListItemDto>> Handle(SetPriceCommand request, CancellationToken ct)
    {
        var priceList = await _db.PriceLists.FirstOrDefaultAsync(p => p.Id == request.PriceListId, ct);
        if (priceList is null)
            return Result.Failure<PriceListItemDto>("Price list not found.");

        var existing = await _db.PriceListItems
            .FirstOrDefaultAsync(i => i.PriceListId == request.PriceListId
                && i.ProductId == request.ProductId
                && i.VariantId == request.VariantId
                && i.MinQty == request.MinQty, ct);

        if (existing is not null)
        {
            _db.PriceListItems.Remove(existing);
        }

        var item = PriceListItem.Create(request.PriceListId, request.ProductId, request.Price, request.MinQty, request.VariantId);
        _db.PriceListItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var productName = await _db.Products.Where(p => p.Id == request.ProductId).Select(p => p.Name).FirstOrDefaultAsync(ct);

        return Result.Success(new PriceListItemDto(
            item.Id, item.PriceListId, item.ProductId, productName,
            item.VariantId, item.Price, item.MinQty));
    }
}
