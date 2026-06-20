using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using GeorgiaERP.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Commands;

public record CreatePriceListCommand(
    string Code,
    string Name,
    string? NameKa,
    string PriceType,
    Guid? StoreId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority) : IRequest<Result<PriceListDto>>;

public class CreatePriceListCommandHandler : IRequestHandler<CreatePriceListCommand, Result<PriceListDto>>
{
    private readonly IAppDbContext _db;

    public CreatePriceListCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PriceListDto>> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        if (await _db.PriceLists.AnyAsync(p => p.Code == request.Code, ct))
            return Result.Failure<PriceListDto>($"Price list with code '{request.Code}' already exists.");

        if (!Enum.TryParse<PriceType>(request.PriceType, true, out var priceType))
            return Result.Failure<PriceListDto>("Invalid price type.");

        var priceList = PriceList.Create(request.Code, request.Name, priceType, request.ValidFrom, request.NameKa);
        _db.PriceLists.Add(priceList);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new PriceListDto(
            priceList.Id, priceList.Code, priceList.Name, priceList.NameKa,
            priceList.Currency, priceList.PriceType.ToString(), priceList.StoreId,
            priceList.ValidFrom, priceList.ValidTo, priceList.IsActive,
            priceList.Priority, 0, priceList.CreatedAt));
    }
}
