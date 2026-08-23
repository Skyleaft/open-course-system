using System.Text.Json.Serialization;

namespace MonoSlice.Shared.Abstractions.Common;

/// <summary>
/// Standard RFC 9110 / RFC 7807 / RFC 9457 problem type URIs.
/// </summary>
public static class ProblemTypes
{
    public const string BadRequest = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    public const string Unauthorized = "https://tools.ietf.org/html/rfc9110#section-15.5.2";
    public const string Forbidden = "https://tools.ietf.org/html/rfc9110#section-15.5.4";
    public const string NotFound = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
    public const string Conflict = "https://tools.ietf.org/html/rfc9110#section-15.5.10";
    public const string UnprocessableEntity = "https://tools.ietf.org/html/rfc9110#section-15.5.21";
    public const string InternalServerError = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
    public const string ValidationError = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
}

/// <summary>
/// Common contract for all API responses supporting both envelope and RFC 7807 / RFC 9457 Problem Details.
/// </summary>
public interface IApiResponse
{
    bool Success { get; }
    string? Message { get; }
    string? Title { get; }
    string? Detail { get; }
    string? Type { get; }
    string? Instance { get; }
    string? Code { get; }
    int Status { get; }
    int StatusCode { get; }
    string? TraceId { get; set; }
    IReadOnlyDictionary<string, string[]>? Errors { get; }
    IReadOnlyList<string>? ErrorList { get; }
}

/// <summary>
/// Standard API response wrapper aligned with RFC 7807 and RFC 9457 Problem Details.
/// </summary>
public class ApiResponse : IApiResponse
{
    public bool Success { get; init; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    public int Status { get; init; } = 200;

    [JsonIgnore]
    public int StatusCode
    {
        get => Status;
        init => Status = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    [JsonIgnore]
    public string? Message
    {
        get => Detail;
        init => Detail = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    [JsonIgnore]
    public IReadOnlyList<string>? ErrorList
    {
        get
        {
            if (Errors is null || Errors.Count == 0)
                return null;

            return Errors.Values.SelectMany(v => v).ToList();
        }
    }

    public static ApiResponse Ok() =>
        new()
        {
            Success = true,
            Status = 200
        };

    public static ApiResponse Ok(string? message) =>
        new()
        {
            Success = true,
            Status = 200,
            Detail = message
        };

    public static ApiResponse Ok(string? message, int statusCode) =>
        new()
        {
            Success = true,
            Status = statusCode,
            Detail = message
        };

    public static ApiResponse Fail(
        string message,
        int statusCode = 400,
        string? code = null,
        string? type = null,
        string? title = null,
        string? instance = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? GetDefaultCode(statusCode),
            Type = type ?? GetDefaultType(statusCode),
            Instance = instance
        };

    public static ApiResponse Fail(
        string message,
        IReadOnlyList<string>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null)
    {
        Dictionary<string, string[]>? dict = null;
        if (errors is not null && errors.Count > 0)
        {
            dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "General", errors.ToArray() }
            };
        }

        return new()
        {
            Success = false,
            Status = statusCode,
            Title = GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? GetDefaultCode(statusCode),
            Type = GetDefaultType(statusCode),
            Errors = dict,
            Instance = instance
        };
    }

    public static ApiResponse Fail(
        string message,
        IReadOnlyDictionary<string, string[]>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? GetDefaultCode(statusCode),
            Type = GetDefaultType(statusCode),
            Errors = errors,
            Instance = instance
        };

    public static ApiResponse Problem(
        int statusCode,
        string title,
        string? detail = null,
        string? type = null,
        string? instance = null,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? GetDefaultType(statusCode),
            Instance = instance,
            Code = code ?? GetDefaultCode(statusCode),
            Errors = errors,
            TraceId = traceId
        };

    public static ApiResponse ValidationProblem(
        IReadOnlyDictionary<string, string[]> errors,
        string? detail = "One or more validation errors occurred.",
        string? instance = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Status = 400,
            Title = "Validation Error",
            Detail = detail,
            Type = ProblemTypes.ValidationError,
            Code = "Validation.Error",
            Instance = instance,
            Errors = errors,
            TraceId = traceId
        };

    public static ApiResponse FromError(
        Error error,
        int? statusCode = null,
        string? instance = null,
        string? traceId = null)
    {
        var resolvedStatusCode = statusCode ?? GetStatusCodeFromErrorType(error.Type);
        return new()
        {
            Success = false,
            Status = resolvedStatusCode,
            Title = GetDefaultTitle(resolvedStatusCode),
            Detail = error.Description,
            Code = error.Code,
            Type = GetDefaultType(resolvedStatusCode),
            Errors = error.ValidationErrors,
            Instance = instance,
            TraceId = traceId
        };
    }

    public static ApiResponse<T> Ok<T>(T data, string? message = null) =>
        ApiResponse<T>.Ok(data, message);

    public static ApiResponse<T> Ok<T>(T data, string? message, int statusCode) =>
        ApiResponse<T>.Ok(data, message, statusCode);

    public static ApiResponse<T> Fail<T>(
        string message,
        int statusCode = 400,
        string? code = null,
        string? type = null,
        string? title = null,
        string? instance = null) =>
        ApiResponse<T>.Fail(message, statusCode, code, type, title, instance);

    public static ApiResponse<T> Fail<T>(
        string message,
        IReadOnlyList<string>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null) =>
        ApiResponse<T>.Fail(message, errors, statusCode, code, instance);

    public static ApiResponse<T> Fail<T>(
        string message,
        IReadOnlyDictionary<string, string[]>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null) =>
        ApiResponse<T>.Fail(message, errors, statusCode, code, instance);

    public static ApiResponse<T> FromError<T>(
        Error error,
        int? statusCode = null,
        string? instance = null,
        string? traceId = null) =>
        ApiResponse<T>.FromError(error, statusCode, instance, traceId);

    public static string GetDefaultTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        408 => "Request Timeout",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        499 => "Client Closed Request",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        _ => "An error occurred"
    };

    public static string GetDefaultType(int statusCode) => statusCode switch
    {
        400 => ProblemTypes.BadRequest,
        401 => ProblemTypes.Unauthorized,
        403 => ProblemTypes.Forbidden,
        404 => ProblemTypes.NotFound,
        409 => ProblemTypes.Conflict,
        422 => ProblemTypes.UnprocessableEntity,
        500 => ProblemTypes.InternalServerError,
        _ => "about:blank"
    };

    public static string GetDefaultCode(int statusCode) => statusCode switch
    {
        400 => "BadRequest",
        401 => "Auth.Unauthorized",
        403 => "Auth.Forbidden",
        404 => "Resource.NotFound",
        409 => "Resource.Conflict",
        422 => "BusinessRule.Violation",
        500 => "InternalServerError",
        _ => "General.Error"
    };

    public static int GetStatusCodeFromErrorType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => 400,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.Critical => 500,
        _ => 400
    };
}

/// <summary>
/// Standard API response wrapper with typed data payload and RFC 7807 / RFC 9457 Problem Details support.
/// </summary>
public class ApiResponse<T> : IApiResponse
{
    public bool Success { get; init; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    public int Status { get; init; } = 200;

    [JsonIgnore]
    public int StatusCode
    {
        get => Status;
        init => Status = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    [JsonIgnore]
    public string? Message
    {
        get => Detail;
        init => Detail = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    [JsonIgnore]
    public IReadOnlyList<string>? ErrorList
    {
        get
        {
            if (Errors is null || Errors.Count == 0)
                return null;

            return Errors.Values.SelectMany(v => v).ToList();
        }
    }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Data = data,
            Status = 200,
            Detail = message
        };

    public static ApiResponse<T> Ok(T data, string? message, int statusCode) =>
        new()
        {
            Success = true,
            Data = data,
            Status = statusCode,
            Detail = message
        };

    public static ApiResponse<T> Fail(
        string message,
        int statusCode = 400,
        string? code = null,
        string? type = null,
        string? title = null,
        string? instance = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = title ?? ApiResponse.GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? ApiResponse.GetDefaultCode(statusCode),
            Type = type ?? ApiResponse.GetDefaultType(statusCode),
            Instance = instance
        };

    public static ApiResponse<T> Fail(
        string message,
        IReadOnlyList<string>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null)
    {
        Dictionary<string, string[]>? dict = null;
        if (errors is not null && errors.Count > 0)
        {
            dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "General", errors.ToArray() }
            };
        }

        return new()
        {
            Success = false,
            Status = statusCode,
            Title = ApiResponse.GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? ApiResponse.GetDefaultCode(statusCode),
            Type = ApiResponse.GetDefaultType(statusCode),
            Errors = dict,
            Instance = instance
        };
    }

    public static ApiResponse<T> Fail(
        string message,
        IReadOnlyDictionary<string, string[]>? errors,
        int statusCode = 400,
        string? code = null,
        string? instance = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = ApiResponse.GetDefaultTitle(statusCode),
            Detail = message,
            Code = code ?? ApiResponse.GetDefaultCode(statusCode),
            Type = ApiResponse.GetDefaultType(statusCode),
            Errors = errors,
            Instance = instance
        };

    public static ApiResponse<T> Problem(
        int statusCode,
        string title,
        string? detail = null,
        string? type = null,
        string? instance = null,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? ApiResponse.GetDefaultType(statusCode),
            Instance = instance,
            Code = code ?? ApiResponse.GetDefaultCode(statusCode),
            Errors = errors,
            TraceId = traceId
        };

    public static ApiResponse<T> ValidationProblem(
        IReadOnlyDictionary<string, string[]> errors,
        string? detail = "One or more validation errors occurred.",
        string? instance = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Status = 400,
            Title = "Validation Error",
            Detail = detail,
            Type = ProblemTypes.ValidationError,
            Code = "Validation.Error",
            Instance = instance,
            Errors = errors,
            TraceId = traceId
        };

    public static ApiResponse<T> FromError(
        Error error,
        int? statusCode = null,
        string? instance = null,
        string? traceId = null)
    {
        var resolvedStatusCode = statusCode ?? ApiResponse.GetStatusCodeFromErrorType(error.Type);
        return new()
        {
            Success = false,
            Status = resolvedStatusCode,
            Title = ApiResponse.GetDefaultTitle(resolvedStatusCode),
            Detail = error.Description,
            Code = error.Code,
            Type = ApiResponse.GetDefaultType(resolvedStatusCode),
            Errors = error.ValidationErrors,
            Instance = instance,
            TraceId = traceId
        };
    }
}

