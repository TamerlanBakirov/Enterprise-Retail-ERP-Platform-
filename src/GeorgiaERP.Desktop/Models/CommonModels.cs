namespace GeorgiaERP.Desktop.Models;

public class ApiResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}

public class ApiResult<T> : ApiResult
{
    public T? Value { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record StoreDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string? Address,
    bool IsActive);

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    Guid? StoreId,
    string? StoreName,
    bool IsActive);

public record LicenseInfo(
    bool IsValid,
    string? CompanyName,
    DateTimeOffset? ExpiresAt,
    int MaxUsers,
    int MaxStores,
    string? Error);
