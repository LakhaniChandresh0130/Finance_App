namespace Finance.Application.Common;

public sealed class Result
{
    public bool Success { get; init; }
    public bool IsConcurrencyConflict { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();

    public static Result Ok() => new() { Success = true };
    public static Result Fail(params string[] errors) => new() { Success = false, Errors = errors };
    public static Result Conflict(params string[] errors) => new()
    {
        Success = false,
        IsConcurrencyConflict = true,
        Errors = errors.Length > 0 ? errors : ["The record was changed by another user. Refresh and try again."]
    };
}

public sealed class Result<T>
{
    public bool Success { get; init; }
    public bool IsConcurrencyConflict { get; init; }
    public T? Value { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static Result<T> Fail(params string[] errors) => new() { Success = false, Errors = errors };
    public static Result<T> Conflict(params string[] errors) => new()
    {
        Success = false,
        IsConcurrencyConflict = true,
        Errors = errors.Length > 0 ? errors : ["The record was changed by another user. Refresh and try again."]
    };
}
