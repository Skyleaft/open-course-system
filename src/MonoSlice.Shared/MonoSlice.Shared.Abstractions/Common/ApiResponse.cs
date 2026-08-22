namespace MonoSlice.Shared.Abstractions.Common;

/// <summary>
/// Common contract for all API responses in the Result pattern.
/// </summary>
public interface IApiResponse
{
    bool Success { get; }
    string? Message { get; }
    IReadOnlyList<string>? Errors { get; }
    int StatusCode { get; }
    string? TraceId { get; set; }
}

/// <summary>
/// Standard API response wrapper for non-generic endpoints.
/// </summary>
public class ApiResponse : IApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public int StatusCode { get; init; } = 200;
    public string? TraceId { get; set; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message, StatusCode = 200 };

    public static ApiResponse Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };

    public static ApiResponse Fail(string message, IReadOnlyList<string> errors, int statusCode = 400) =>
        new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };

    public static ApiResponse<T> Ok<T>(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Fail<T>(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };

    public static ApiResponse<T> Fail<T>(string message, IReadOnlyList<string>? errors, int statusCode = 400) =>
        new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
}

/// <summary>
/// Standard API response wrapper with typed data payload.
/// </summary>
public class ApiResponse<T> : IApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public int StatusCode { get; init; } = 200;
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors, int statusCode = 400) =>
        new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
}

