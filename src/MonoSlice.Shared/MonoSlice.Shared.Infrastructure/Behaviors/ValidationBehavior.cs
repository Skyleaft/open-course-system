using System.ComponentModel.DataAnnotations;
using Mediator;
using MonoSlice.Shared.Abstractions.Common;
using ValidationException = MonoSlice.Shared.Abstractions.Exceptions.ValidationException;

namespace MonoSlice.Shared.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that validates messages using DataAnnotations.
/// Adopts the Result Pattern and RFC 7807 / RFC 9457 standard validation structure.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Compile-Time Sannr Validation (AOT & Trim-Safe)
        if (message is not null && Sannr.SannrValidatorRegistry.TryGetValidator(typeof(TMessage), out var sannrValidator))
        {
            var sannrContext = new Sannr.SannrValidationContext(message, serviceProvider: null, items: null, group: null);
            var sannrResult = await sannrValidator(sannrContext);

            if (sannrResult is not null && !sannrResult.IsValid)
            {
                var validationErrors = sannrResult.Errors
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.MemberName) ? "General" : e.MemberName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.Message).Distinct().ToArray(),
                        StringComparer.OrdinalIgnoreCase);

                if (TryCreateValidationFailureResult(validationErrors, out var failureResponse))
                {
                    return failureResponse;
                }

                throw new ValidationException(validationErrors, "Validation failed.");
            }

            return await next(message, cancellationToken);
        }

        if (message is null)
        {
            return await next(message!, cancellationToken);
        }

        // 2. Fallback to System.ComponentModel.DataAnnotations
        var context = new ValidationContext(message, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(message, context, validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var validationErrors = validationResults
                .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : ["General"])
                    .Select(m => new { Member = m, Error = r.ErrorMessage ?? "Validation error occurred." }))
                .GroupBy(x => x.Member)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Error).Distinct().ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            // Result Pattern: return failure response directly instead of throwing an exception
            if (TryCreateValidationFailureResult(validationErrors, out var failureResponse))
            {
                return failureResponse;
            }

            // Fallback for non-Result response types
            throw new ValidationException(validationErrors, "Validation failed.");
        }

        return await next(message, cancellationToken);
    }

    private static bool TryCreateValidationFailureResult(
        Dictionary<string, string[]> validationErrors,
        out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(ApiResponse))
        {
            response = (TResponse)(object)ApiResponse.ValidationProblem(validationErrors, "Validation failed.");
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var problemMethod = typeof(ApiResponse<>)
                .MakeGenericType(dataType)
                .GetMethod(
                    nameof(ApiResponse<object>.ValidationProblem),
                    [typeof(IReadOnlyDictionary<string, string[]>), typeof(string), typeof(string), typeof(string)]);

            if (problemMethod is not null)
            {
                var result = problemMethod.Invoke(null, [validationErrors, "Validation failed.", null, null]);
                if (result is TResponse typedResult)
                {
                    response = typedResult;
                    return true;
                }
            }
        }

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure("Validation.Error", "Validation failed.", validationErrors);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result<>)
                .MakeGenericType(dataType)
                .GetMethod(
                    nameof(Result<object>.Failure),
                    [typeof(string), typeof(string), typeof(IReadOnlyDictionary<string, string[]>)]);

            if (failMethod is not null)
            {
                var result = failMethod.Invoke(null, ["Validation.Error", "Validation failed.", validationErrors]);
                if (result is TResponse typedResult)
                {
                    response = typedResult;
                    return true;
                }
            }
        }

        response = default!;
        return false;
    }
}


