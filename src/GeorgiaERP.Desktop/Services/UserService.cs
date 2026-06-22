using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IUserService
{
    Task<PagedResult<UserListDto>> GetUsersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<UserListDto?> CreateUserAsync(CreateUserRequest request);
}

public class UserService : IUserService
{
    private readonly IApiClient _api;
    public UserService(IApiClient api) => _api = api;

    public Task<PagedResult<UserListDto>> GetUsersAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var q = $"users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return _api.GetAsync<PagedResult<UserListDto>>(q)!;
    }

    public Task<UserListDto?> CreateUserAsync(CreateUserRequest request) =>
        _api.PostAsync<CreateUserRequest, UserListDto>("users", request);
}
