using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Organization.DTOs;
using GeorgiaERP.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Organization.Commands;

public record CreateCompanyCommand(
    string Code,
    string Name,
    string? NameKa,
    string Tin,
    bool IsVatPayer,
    string? LegalAddress,
    string? ActualAddress,
    string? Phone,
    string? Email) : IRequest<Result<CompanyDto>>;

public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string? NameKa,
    string? LegalAddress,
    string? ActualAddress,
    string? Phone,
    string? Email) : IRequest<Result>;

public record CreateStoreCommand(
    string Code,
    string Name,
    string? NameKa,
    string StoreType,
    string? Address,
    string? City,
    string? Region,
    string? Phone,
    Guid? ManagerUserId) : IRequest<Result<StoreDto>>;

public record UpdateStoreCommand(
    Guid Id,
    string Name,
    string? NameKa,
    string? Address,
    string? City,
    string? Region,
    string? Phone,
    Guid? ManagerUserId) : IRequest<Result>;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    private readonly IAppDbContext _db;
    public CreateCompanyCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        if (await _db.Companies.AnyAsync(c => c.Code == request.Code, ct))
            return Result.Failure<CompanyDto>($"Company with code '{request.Code}' already exists.");

        if (await _db.Companies.AnyAsync(c => c.Tin == request.Tin, ct))
            return Result.Failure<CompanyDto>($"Company with TIN '{request.Tin}' already exists.");

        var company = Company.Create(request.Code, request.Name, request.Tin, request.IsVatPayer, request.NameKa);
        company.Update(request.Name, request.NameKa, request.LegalAddress, request.ActualAddress, request.Phone, request.Email);

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new CompanyDto(
            company.Id, company.Code, company.Name, company.NameKa,
            company.Tin, company.IsVatPayer, company.VatRegistrationDate,
            company.LegalAddress, company.ActualAddress, company.Phone, company.Email,
            company.IsActive, company.CreatedAt));
    }
}

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result>
{
    private readonly IAppDbContext _db;
    public UpdateCompanyCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (company is null) return Result.Failure("Company not found.", "NOT_FOUND");

        company.Update(request.Name, request.NameKa, request.LegalAddress, request.ActualAddress, request.Phone, request.Email);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<StoreDto>>
{
    private readonly IAppDbContext _db;
    public CreateStoreCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<StoreDto>> Handle(CreateStoreCommand request, CancellationToken ct)
    {
        if (await _db.Stores.AnyAsync(s => s.Code == request.Code, ct))
            return Result.Failure<StoreDto>($"Store with code '{request.Code}' already exists.");

        if (!Enum.TryParse<StoreType>(request.StoreType, true, out var storeType))
            return Result.Failure<StoreDto>($"Invalid store type '{request.StoreType}'.");

        var store = Store.Create(request.Code, request.Name, storeType, request.NameKa);
        store.Update(request.Name, request.NameKa, request.Address, request.City, request.Region, request.Phone);

        if (request.ManagerUserId.HasValue)
            store.SetManager(request.ManagerUserId);

        _db.Stores.Add(store);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new StoreDto(
            store.Id, store.Code, store.Name, store.NameKa,
            store.StoreType.ToString(), store.Address, store.City, store.Region,
            store.Phone, store.ManagerUserId, store.IsActive, store.CreatedAt));
    }
}

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, Result>
{
    private readonly IAppDbContext _db;
    public UpdateStoreCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateStoreCommand request, CancellationToken ct)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (store is null) return Result.Failure("Store not found.", "NOT_FOUND");

        store.Update(request.Name, request.NameKa, request.Address, request.City, request.Region, request.Phone);

        if (request.ManagerUserId.HasValue)
            store.SetManager(request.ManagerUserId);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
