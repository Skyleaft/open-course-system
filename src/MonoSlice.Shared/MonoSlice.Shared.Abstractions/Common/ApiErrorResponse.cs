namespace MonoSlice.Shared.Abstractions.Common;

/// <summary>
/// Standard API error envelope with structured error codes and validation details.
/// </summary>
public class ApiErrorResponse : IApiResponse
{
    public bool Success => false;
    public string Message { get; init; } = "An error occurred.";
    public string Code { get; init; } = "INTERNAL_ERROR";
    public int StatusCode { get; init; } = 400;
    public IReadOnlyList<string>? Errors { get; init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }
    public string? TraceId { get; set; }

    public static ApiErrorResponse Validation(
        IReadOnlyDictionary<string, string[]> validationErrors, 
        string message = "One or more validation errors occurred.") =>
        new()
        {
            Code = "VALIDATION_ERROR",
            Message = message,
            StatusCode = 400,
            ValidationErrors = validationErrors,
            Errors = validationErrors.Values.SelectMany(v => v).ToList()
        };

    public static ApiErrorResponse NotFound(string message = "The requested resource was not found.") =>
        new()
        {
            Code = "NOT_FOUND",
            Message = message,
            StatusCode = 404
        };

    public static ApiErrorResponse Unauthorized(string message = "Authentication credentials were missing or invalid.") =>
        new()
        {
            Code = "UNAUTHORIZED",
            Message = message,
            StatusCode = 401
        };

    public static ApiErrorResponse Forbidden(string message = "Access to the requested resource is denied.") =>
        new()
        {
            Code = "FORBIDDEN",
            Message = message,
            StatusCode = 403
        };

    public static ApiErrorResponse Conflict(string message = "The request could not be completed due to a conflict with current state.") =>
        new()
        {
            Code = "CONFLICT",
            Message = message,
            StatusCode = 409
        };

    public static ApiErrorResponse InternalError(string message = "An internal server error occurred.") =>
        new()
        {
            Code = "INTERNAL_ERROR",
            Message = message,
            StatusCode = 500
        };
}
