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

        var (statusCode, response) = exception switch
        {
            BadHttpRequestException badHttp => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(badHttp.InnerException is JsonException jsonEx
                    ? $"Malformed request payload: {jsonEx.Message}"
                    : badHttp.Message, statusCode: 400)),

            JsonException jsonException => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail($"Invalid JSON payload: {jsonException.Message}", statusCode: 400)),

            FormatException formatException => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail($"Invalid parameter format: {formatException.Message}", statusCode: 400)),

            ValidationException validation => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(validation.Message, validation.Errors, statusCode: 400)),

            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                ApiResponse.Fail(notFound.Message, statusCode: 404)),

            BusinessRuleException business => (
                HttpStatusCode.UnprocessableEntity,
                ApiResponse.Fail(business.Message, statusCode: 422)),

            ForbiddenException forbidden => (
                HttpStatusCode.Forbidden,
                ApiResponse.Fail(forbidden.Message, statusCode: 403)),

            UnauthorizedAccessException unauthorized => (
                HttpStatusCode.Unauthorized,
                ApiResponse.Fail(unauthorized.Message, statusCode: 401)),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                ApiResponse.Fail("A concurrency conflict occurred. The resource was modified or deleted by another operation.", statusCode: 409)),

            DbUpdateException dbEx => (
                HttpStatusCode.Conflict,
                ApiResponse.Fail(isDevelopment ? $"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}" : "A database constraint violation occurred.", statusCode: 409)),

            TimeoutException or TaskCanceledException when !context.RequestAborted.IsCancellationRequested => (
                HttpStatusCode.RequestTimeout,
                ApiResponse.Fail("The requested operation timed out.", statusCode: 408)),

            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (
                (HttpStatusCode)499, // Client Closed Request
                ApiResponse.Fail("The client cancelled the request.", statusCode: 499)),

            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                ApiResponse.Fail("The requested feature is not implemented.", statusCode: 501)),

            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.Fail(isDevelopment
                    ? $"An unexpected error occurred: [{exception.GetType().Name}] {exception.Message} | StackTrace: {exception.StackTrace}"
                    : "An unexpected error occurred. Please try again later.", statusCode: 500))
        };

        response.TraceId = traceId;

        var httpStatusCode = (int)statusCode;
        if (httpStatusCode >= 500)
        {
            _logger.LogError(exception, "[{TraceId}] HTTP {Method} {Path} failed with {StatusCode}: {Message}",
                traceId, context.Request.Method, context.Request.Path, httpStatusCode, exception.Message);
        }
        else
        {
            _logger.LogWarning("[{TraceId}] Handled client exception [{StatusCode}] on HTTP {Method} {Path}: {Message}",
                traceId, httpStatusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = httpStatusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SharedJsonSerializerContext.DefaultOptions));
    }
}

