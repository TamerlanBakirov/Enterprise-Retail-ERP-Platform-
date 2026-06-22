using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Webhooks.DTOs;
using GeorgiaERP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Webhooks.Commands;

public record CreateWebhookCommand(
    string Name, string Url, string Secret, List<string> EventTypes, int MaxRetries = 3)
    : IRequest<Result<WebhookSubscriptionDto>>;

public record UpdateWebhookCommand(
    Guid Id, string? Name, string? Url, List<string>? EventTypes, int? MaxRetries)
    : IRequest<Result>;

public record DeleteWebhookCommand(Guid Id) : IRequest<Result>;

public record ActivateWebhookCommand(Guid Id) : IRequest<Result>;

public record DeactivateWebhookCommand(Guid Id) : IRequest<Result>;

public class CreateWebhookCommandHandler : IRequestHandler<CreateWebhookCommand, Result<WebhookSubscriptionDto>>
{
    private readonly IAppDbContext _db;

    public CreateWebhookCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<WebhookSubscriptionDto>> Handle(CreateWebhookCommand request, CancellationToken cancellationToken)
    {
        var subscription = WebhookSubscription.Create(
            request.Name, request.Url, request.Secret, request.EventTypes, request.MaxRetries);

        _db.WebhookSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(subscription));
    }

    private static WebhookSubscriptionDto MapToDto(WebhookSubscription s) =>
        new(s.Id, s.Name, s.Url, s.GetEventTypes(), s.IsActive,
            s.MaxRetries, s.ConsecutiveFailures, s.LastDeliveryAt, s.LastDeliveryStatus, s.CreatedAt);
}

public class UpdateWebhookCommandHandler : IRequestHandler<UpdateWebhookCommand, Result>
{
    private readonly IAppDbContext _db;

    public UpdateWebhookCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateWebhookCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription is null)
            return Result.NotFound("WebhookSubscription", request.Id);

        subscription.Update(request.Name, request.Url, request.EventTypes, request.MaxRetries);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class DeleteWebhookCommandHandler : IRequestHandler<DeleteWebhookCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteWebhookCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteWebhookCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription is null)
            return Result.NotFound("WebhookSubscription", request.Id);

        _db.WebhookSubscriptions.Remove(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class ActivateWebhookCommandHandler : IRequestHandler<ActivateWebhookCommand, Result>
{
    private readonly IAppDbContext _db;

    public ActivateWebhookCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(ActivateWebhookCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription is null)
            return Result.NotFound("WebhookSubscription", request.Id);

        subscription.Activate();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class DeactivateWebhookCommandHandler : IRequestHandler<DeactivateWebhookCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeactivateWebhookCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeactivateWebhookCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription is null)
            return Result.NotFound("WebhookSubscription", request.Id);

        subscription.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
