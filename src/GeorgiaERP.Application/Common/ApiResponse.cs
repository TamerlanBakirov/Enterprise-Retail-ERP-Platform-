namespace GeorgiaERP.Application.Common;

/// <summary>
/// Standardized API response wrapper for all endpoints.
/// Provides a consistent contract for both success and error responses.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse Ok() => new() { Success = true };

    public static ApiResponse Fail(string error) =>
        new() { Success = false, Errors = [error] };

    public static ApiResponse Fail(IReadOnlyList<string> errors) =>
        new() { Success = false, Errors = errors };

    public static ApiResponse<T> Ok<T>(T data) =>
        new() { Success = true, Data = data };

    public static ApiResponse<T> Fail<T>(string error) =>
        new() { Success = false, Errors = [error] };

    public static ApiResponse<T> Fail<T>(IReadOnlyList<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Typed API response wrapper containing a data payload.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}
