namespace GeorgiaERP.Application.Organization.DTOs;

public record CompanyDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string Tin,
    bool IsVatPayer,
    DateTimeOffset? VatRegistrationDate,
    string? LegalAddress,
    string? ActualAddress,
    string? Phone,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record StoreDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string StoreType,
    string? Address,
    string? City,
    string? Region,
    string? Phone,
    Guid? ManagerUserId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string WarehouseType,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateStoreRequest(
    string Code,
    string Name,
    string? NameKa,
    string StoreType,
    string? Address,
    string? City,
    string? Region,
    string? Phone,
    Guid? ManagerUserId);

public record CreateWarehouseRequest(
    string Code,
    string Name,
    string? NameKa,
    string WarehouseType,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId);
