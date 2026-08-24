using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Infrastructure.Serialization;

namespace MonoSlice.Shared.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware implementing RFC 7807 / RFC 9457 Problem Details.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var env = context.RequestServices.GetService<IHostEnvironment>();
        var isDevelopment = env?.IsDevelopment() ?? false;
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var instance = context.Request.Path.Value ?? "/";

        var (statusCode, title, type, code, detail, errors) = exception switch
        {
            BadHttpRequestException badHttp => (
                400,
                "Bad Request",
                ProblemTypes.BadRequest,
                "BadHttpRequest",
                badHttp.InnerException is JsonException jsonEx
                    ? $"Malformed request payload: {jsonEx.Message}"
                    : badHttp.Message,
                (IReadOnlyDictionary<string, string[]>?)null),

            JsonException jsonException => (
                400,
                "Invalid JSON Payload",
                ProblemTypes.BadRequest,
                "InvalidJson",
                $"Invalid JSON payload: {jsonException.Message}",
                null),

            FormatException formatException => (
                400,
                "Invalid Parameter Format",
                ProblemTypes.BadRequest,
                "InvalidFormat",
                $"Invalid parameter format: {formatException.Message}",
                null),

            ValidationException validation => (
                validation.StatusCode,
                validation.Title,
                validation.Type ?? ProblemTypes.ValidationError,
                validation.Code,
                validation.Message,
                (IReadOnlyDictionary<string, string[]>?)validation.Errors),

            AppException appEx => (
                appEx.StatusCode,
                appEx.Title,
                appEx.Type ?? ApiResponse.GetDefaultType(appEx.StatusCode),
                appEx.Code,
                appEx.Message,
                appEx.ValidationErrors),

            UnauthorizedAccessException unauthorized => (
                401,
                "Unauthorized",
                ProblemTypes.Unauthorized,
                "Auth.Unauthorized",
                unauthorized.Message,
                null),

            DbUpdateConcurrencyException => (
                409,
                "Concurrency Conflict",
                ProblemTypes.Conflict,
                "Database.ConcurrencyConflict",
                "A concurrency conflict occurred. The resource was modified or deleted by another operation.",
                null),

            DbUpdateException dbEx => (
                409,
                "Database Conflict",
                ProblemTypes.Conflict,
                "Database.ConstraintViolation",
                isDevelopment
                    ? $"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}"
                    : "A database constraint violation occurred.",
                null),

            TimeoutException or TaskCanceledException when !context.RequestAborted.IsCancellationRequested => (
                408,
                "Request Timeout",
                ProblemTypes.InternalServerError,
                "Request.Timeout",
                "The requested operation timed out.",
                null),

            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (
                499, // Client Closed Request
                "Client Closed Request",
                "about:blank",
                "Client.ClosedRequest",
                "The client cancelled the request.",
                null),

            NotImplementedException => (
                501,
                "Not Implemented",
                ProblemTypes.InternalServerError,
                "Server.NotImplemented",
                "The requested feature is not implemented.",
                null),

            _ => (
                500,
                "Internal Server Error",
                ProblemTypes.InternalServerError,
                "Server.InternalError",
                isDevelopment
                    ? $"An unexpected error occurred: [{exception.GetType().Name}] {exception.Message} | StackTrace: {exception.StackTrace}"
                    : "An unexpected error occurred. Please try again later.",
                null)
        };

        var response = new ApiResponse
        {
            Success = false,
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = instance,
            Code = code,
            Errors = errors,
            TraceId = traceId
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "[{TraceId}] HTTP {Method} {Path} failed with {StatusCode} ({Code}): {Message}",
                traceId, context.Request.Method, context.Request.Path, statusCode, code, detail);
        }
        else
        {
            _logger.LogWarning("[{TraceId}] Handled client exception [{StatusCode}] ({Code}) on HTTP {Method} {Path}: {Message}",
                traceId, statusCode, code, context.Request.Method, context.Request.Path, detail);
        }

        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SharedJsonSerializerContext.DefaultOptions));
    }
}


