namespace GeorgiaERP.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error, string? errorCode = null, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        Errors = errors ?? (error is not null ? [error] : []);
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(string error, string errorCode) => new(false, error, errorCode);
    public static Result ValidationFailure(IReadOnlyList<string> errors) => new(false, errors.FirstOrDefault(), "VALIDATION_ERROR", errors);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
    public static Result<T> Failure<T>(string error, string errorCode) => new(default, false, error, errorCode);
    public static Result<T> ValidationFailure<T>(IReadOnlyList<string> errors) => new(default, false, errors.FirstOrDefault(), "VALIDATION_ERROR", errors);
    public static Result<T> NotFound<T>(string entity, object id) => new(default, false, $"{entity} with ID '{id}' was not found.", "NOT_FOUND");
    public static Result NotFound(string entity, object id) => new(false, $"{entity} with ID '{id}' was not found.", "NOT_FOUND");
    public static Result Conflict(string error) => new(false, error, "CONFLICT");
    public static Result<T> Conflict<T>(string error) => new(default, false, error, "CONFLICT");
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error, string? errorCode = null, IReadOnlyList<string>? errors = null)
        : base(isSuccess, error, errorCode, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Maps the value of a successful result to a new type. Propagates failure as-is.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess
            ? Result.Success(mapper(Value!))
            : new Result<TOut>(default, false, Error, ErrorCode, Errors);
    }
}
