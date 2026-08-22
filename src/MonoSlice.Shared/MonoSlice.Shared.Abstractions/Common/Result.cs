namespace MonoSlice.Shared.Abstractions.Common;

/// <summary>
/// Domain error representation for the Result pattern.
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error Failure(string code, string description) => new(code, description);
    public static Error Validation(string description) => new("Validation.Error", description);
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
    public static Error Unauthorized(string description) => new("Auth.Unauthorized", description);
    public static Error Forbidden(string description) => new("Auth.Forbidden", description);
}

/// <summary>
/// Result pattern object representing the outcome of an operation without throwing exceptions.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, Error error, IReadOnlyList<string>? errors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot contain an error.");

        if (!isSuccess && error == Error.None && (errors == null || errors.Count == 0))
            throw new InvalidOperationException("Failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? (error != Error.None ? [error.Description] : []);
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(false, Error.Failure("General.Failure", message), errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string message, IReadOnlyList<string>? errors = null) =>
        Result<T>.Failure(message, errors);
}

/// <summary>
/// Typed generic Result pattern containing a payload upon success.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, Error error, IReadOnlyList<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public new static Result<T> Failure(Error error) => new(default, false, error);
    public new static Result<T> Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(default, false, Error.Failure("General.Failure", message), errors);

    public static implicit operator Result<T>(T value) => Success(value);
}
