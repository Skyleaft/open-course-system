namespace MonoSlice.Shared.Abstractions.Common;

/// <summary>
/// Error type categorization matching RFC 7807 / RFC 9457 semantics.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Critical = 6
}

/// <summary>
/// Domain error representation for the Result pattern aligned with RFC 7807 / RFC 9457.
/// </summary>
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type = ErrorType.Failure,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.", ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string description, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new("Validation.Error", description, ErrorType.Validation, validationErrors);

    public static Error Validation(string code, string description, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(code, description, ErrorType.Validation, validationErrors);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string description, string code = "Auth.Unauthorized") =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string description, string code = "Auth.Forbidden") =>
        new(code, description, ErrorType.Forbidden);

    public static Error Critical(string code, string description) =>
        new(code, description, ErrorType.Critical);
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
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(
        bool isSuccess,
        Error error,
        IReadOnlyList<string>? errors = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot contain an error.");

        if (!isSuccess && error == Error.None && (errors == null || errors.Count == 0) && (validationErrors == null || validationErrors.Count == 0))
            throw new InvalidOperationException("Failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? error.ValidationErrors;

        if (errors is not null && errors.Count > 0)
        {
            Errors = errors;
        }
        else if (ValidationErrors is not null && ValidationErrors.Count > 0)
        {
            Errors = ValidationErrors.Values.SelectMany(v => v).ToList();
        }
        else if (error != Error.None && !string.IsNullOrWhiteSpace(error.Description))
        {
            Errors = [error.Description];
        }
        else
        {
            Errors = [];
        }
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(false, Error.Failure("General.Failure", message), errors);
    public static Result Failure(string message, IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(false, Error.Validation(message, validationErrors), validationErrors: validationErrors);
    public static Result Failure(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(false, validationErrors is not null && validationErrors.Count > 0 ? Error.Validation(code, message, validationErrors) : Error.Failure(code, message), validationErrors: validationErrors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string message, IReadOnlyList<string>? errors = null) =>
        Result<T>.Failure(message, errors);
    public static Result<T> Failure<T>(string message, IReadOnlyDictionary<string, string[]> validationErrors) =>
        Result<T>.Failure(message, validationErrors);
    public static Result<T> Failure<T>(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        Result<T>.Failure(code, message, validationErrors);
}

/// <summary>
/// Typed generic Result pattern containing a payload upon success.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(
        T? value,
        bool isSuccess,
        Error error,
        IReadOnlyList<string>? errors = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, errors, validationErrors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public new static Result<T> Failure(Error error) => new(default, false, error);
    public new static Result<T> Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(default, false, Error.Failure("General.Failure", message), errors);
    public new static Result<T> Failure(string message, IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(default, false, Error.Validation(message, validationErrors), validationErrors: validationErrors);
    public new static Result<T> Failure(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(default, false, validationErrors is not null && validationErrors.Count > 0 ? Error.Validation(code, message, validationErrors) : Error.Failure(code, message), validationErrors: validationErrors);

    public static implicit operator Result<T>(T value) => Success(value);
}
