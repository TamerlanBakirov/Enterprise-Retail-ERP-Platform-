using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Commands;

/// <summary>
/// Activates or deactivates a promotion. Deactivation stops a running promotion
/// early (e.g. budget exhausted) while preserving its record and usage counts.
/// </summary>
public record SetPromotionStatusCommand(Guid Id, bool IsActive) : IRequest<Result<PromotionDto>>;

public class SetPromotionStatusCommandHandler : IRequestHandler<SetPromotionStatusCommand, Result<PromotionDto>>
{
    private readonly IAppDbContext _dbContext;
    public SetPromotionStatusCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PromotionDto>> Handle(SetPromotionStatusCommand request, CancellationToken ct)
    {
        var promotion = await _dbContext.Promotions.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (promotion is null)
            return Result.NotFound<PromotionDto>("Promotion", request.Id);

        if (request.IsActive) promotion.Activate();
        else promotion.Deactivate();

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new PromotionDto(
            promotion.Id, promotion.Code, promotion.Name, promotion.NameKa,
            promotion.PromotionType.ToString(), promotion.DiscountValue, promotion.Conditions,
            promotion.ValidFrom, promotion.ValidTo, promotion.IsActive,
            promotion.MaxUses, promotion.CurrentUses, promotion.CreatedAt));
    }
}
