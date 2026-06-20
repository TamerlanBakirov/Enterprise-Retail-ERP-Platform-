using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using GeorgiaERP.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Commands;

public record CreatePromotionCommand(
    string Code,
    string Name,
    string? NameKa,
    string PromotionType,
    decimal? DiscountValue,
    string? Conditions,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int? MaxUses) : IRequest<Result<PromotionDto>>;

public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, Result<PromotionDto>>
{
    private readonly IAppDbContext _db;

    public CreatePromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PromotionDto>> Handle(CreatePromotionCommand request, CancellationToken ct)
    {
        if (await _db.Promotions.AnyAsync(p => p.Code == request.Code, ct))
            return Result.Failure<PromotionDto>($"Promotion with code '{request.Code}' already exists.");

        if (!Enum.TryParse<PromotionType>(request.PromotionType, true, out var promoType))
            return Result.Failure<PromotionDto>("Invalid promotion type.");

        var promo = Promotion.Create(request.Code, request.Name, promoType, request.ValidFrom, request.DiscountValue, request.NameKa);
        _db.Promotions.Add(promo);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new PromotionDto(
            promo.Id, promo.Code, promo.Name, promo.NameKa,
            promo.PromotionType.ToString(), promo.DiscountValue, promo.Conditions,
            promo.ValidFrom, promo.ValidTo, promo.IsActive,
            promo.MaxUses, promo.CurrentUses, promo.CreatedAt));
    }
}
