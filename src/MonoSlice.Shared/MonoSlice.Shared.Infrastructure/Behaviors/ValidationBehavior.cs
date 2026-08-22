using System.ComponentModel.DataAnnotations;
using Mediator;
using MonoSlice.Shared.Abstractions.Common;
using ValidationException = MonoSlice.Shared.Abstractions.Exceptions.ValidationException;

namespace MonoSlice.Shared.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that validates messages using DataAnnotations.
/// Adopts the Result Pattern: returns failure ApiResponse/Result without throwing exceptions when possible.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext(message, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(message, context, validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var errors = validationResults
                .Select(r => r.ErrorMessage ?? "Validation error occurred.")
                .ToList();

            // Result Pattern: return failure response directly instead of throwing an exception
            if (TryCreateValidationFailureResult(errors, out var failureResponse))
            {
                return failureResponse;
            }

            // Fallback for non-Result response types
            throw new ValidationException(errors);
        }

        return await next(message, cancellationToken);
    }

    private static bool TryCreateValidationFailureResult(List<string> errors, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(ApiResponse))
        {
            response = (TResponse)(object)ApiResponse.Fail("Validation failed.", errors, statusCode: 400);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(ApiResponse<>)
                .MakeGenericType(dataType)
                .GetMethod(nameof(ApiResponse<object>.Fail), [typeof(string), typeof(IReadOnlyList<string>), typeof(int)]);

            if (failMethod is not null)
            {
                var result = failMethod.Invoke(null, ["Validation failed.", errors, 400]);
                if (result is TResponse typedResult)
                {
                    response = typedResult;
                    return true;
                }
            }
        }

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure("Validation failed.", errors);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result<>)
                .MakeGenericType(dataType)
                .GetMethod(nameof(Result<object>.Failure), [typeof(string), typeof(IReadOnlyList<string>)]);

            if (failMethod is not null)
            {
                var result = failMethod.Invoke(null, ["Validation failed.", errors]);
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

