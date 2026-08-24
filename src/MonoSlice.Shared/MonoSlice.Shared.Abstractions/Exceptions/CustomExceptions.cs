using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Shared.Abstractions.Exceptions;

/// <summary>
/// Base exception class for all domain and application exceptions with RFC 7807 / RFC 9457 metadata.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string Code { get; }
    public string? Type { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    protected AppException(
        string message,
        int statusCode = 500,
        string? title = null,
        string? code = null,
        string? type = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Title = title ?? ApiResponse.GetDefaultTitle(statusCode);
        Code = code ?? ApiResponse.GetDefaultCode(statusCode);
        Type = type ?? ApiResponse.GetDefaultType(statusCode);
        ValidationErrors = validationErrors;
    }
}

/// <summary>
/// Thrown when a requested entity or resource is not found (404).
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base(
            $"Entity '{entityName}' with key '{key}' was not found.",
            statusCode: 404,
            title: "Not Found",
            code: $"{entityName}.NotFound",
            type: ProblemTypes.NotFound)
    {
    }

    public NotFoundException(string message)
        : base(
            message,
            statusCode: 404,
            title: "Not Found",
            code: "Resource.NotFound",
            type: ProblemTypes.NotFound)
    {
    }

    public NotFoundException(string code, string message)
        : base(
            message,
            statusCode: 404,
            title: "Not Found",
            code: code,
            type: ProblemTypes.NotFound)
    {
    }
}

/// <summary>
/// Thrown when one or more validation rules are violated (400).
/// </summary>
public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public IReadOnlyList<string> ErrorMessages { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors, string? message = null)
        : base(
            message ?? "One or more validation errors occurred.",
            statusCode: 400,
            title: "Validation Error",
            code: "Validation.Error",
            type: ProblemTypes.ValidationError,
            validationErrors: errors)
    {
        Errors = errors;
        ErrorMessages = errors.Values.SelectMany(v => v).ToList();
    }

    public ValidationException(IReadOnlyList<string> errors, string? message = null)
        : this(
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "General", errors.ToArray() } },
            message)
    {
    }

    public ValidationException(string error)
        : this([error])
    {
    }

    public ValidationException(string propertyName, string error)
        : this(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { propertyName, [error] } })
    {
    }
}

/// <summary>
/// Thrown when a business rule is violated (422).
/// </summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? code = null)
        : base(
            message,
            statusCode: 422,
            title: "Unprocessable Entity",
            code: code ?? "BusinessRule.Violation",
            type: ProblemTypes.UnprocessableEntity)
    {
    }
}

/// <summary>
/// Thrown when a resource conflict occurs, such as concurrency or duplicate unique keys (409).
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message, string? code = null)
        : base(
            message,
            statusCode: 409,
            title: "Conflict",
            code: code ?? "Resource.Conflict",
            type: ProblemTypes.Conflict)
    {
    }
}

/// <summary>
/// Thrown when a user is authenticated but not authorized to perform an action (403).
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string? message = null, string? code = null)
        : base(
            message ?? "You are not authorized to perform this action.",
            statusCode: 403,
            title: "Forbidden",
            code: code ?? "Auth.Forbidden",
            type: ProblemTypes.Forbidden)
    {
    }
}

/// <summary>
/// Thrown when authentication is required or invalid (401).
/// </summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string? message = null, string? code = null)
        : base(
            message ?? "Authentication is required to access this resource.",
            statusCode: 401,
            title: "Unauthorized",
            code: code ?? "Auth.Unauthorized",
            type: ProblemTypes.Unauthorized)
    {
    }
}
