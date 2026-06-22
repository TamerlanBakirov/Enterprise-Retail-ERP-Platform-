using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Commands;

public record CreateTerminalCommand(
    string Code,
    Guid StoreId,
    string Name,
    string TerminalType) : IRequest<Result<TerminalCreatedResponse>>;

public record TerminalCreatedResponse(Guid Id, string Code, string Name);

public class CreateTerminalCommandHandler : IRequestHandler<CreateTerminalCommand, Result<TerminalCreatedResponse>>
{
    private readonly IAppDbContext _db;
    public CreateTerminalCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<TerminalCreatedResponse>> Handle(CreateTerminalCommand request, CancellationToken ct)
    {
        if (await _db.PosTerminals.AnyAsync(t => t.Code == request.Code, ct))
            return Result.Failure<TerminalCreatedResponse>($"Terminal with code '{request.Code}' already exists.");

        var storeExists = await _db.Stores.AnyAsync(s => s.Id == request.StoreId && s.IsActive, ct);
        if (!storeExists)
            return Result.Failure<TerminalCreatedResponse>("Store not found or inactive.");

        if (!Enum.TryParse<TerminalType>(request.TerminalType, true, out var termType))
            return Result.Failure<TerminalCreatedResponse>($"Invalid terminal type '{request.TerminalType}'.");

        var terminal = PosTerminal.Create(request.Code, request.StoreId, request.Name, termType);
        _db.PosTerminals.Add(terminal);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new TerminalCreatedResponse(terminal.Id, terminal.Code, terminal.Name));
    }
}

public record GetTerminalsQuery(Guid? StoreId = null, bool? IsActive = null)
    : IRequest<IReadOnlyList<TerminalDto>>;

public record TerminalDto(
    Guid Id, string Code, string Name, Guid StoreId,
    string TerminalType, bool IsActive, DateTimeOffset CreatedAt);

public class GetTerminalsQueryHandler : IRequestHandler<GetTerminalsQuery, IReadOnlyList<TerminalDto>>
{
    private readonly IAppDbContext _db;
    public GetTerminalsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TerminalDto>> Handle(GetTerminalsQuery request, CancellationToken ct)
    {
        var query = _db.PosTerminals.AsNoTracking();

        if (request.StoreId.HasValue)
            query = query.Where(t => t.StoreId == request.StoreId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(t => t.Code)
            .Select(t => new TerminalDto(
                t.Id, t.Code, t.Name, t.StoreId,
                t.TerminalType.ToString(), t.IsActive, t.CreatedAt))
            .ToListAsync(ct);
    }
}
