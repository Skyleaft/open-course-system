using System.ComponentModel.DataAnnotations;
using Mediator;
using MonoSlice.Shared.Abstractions.Common;
using ValidationException = MonoSlice.Shared.Abstractions.Exceptions.ValidationException;

namespace MonoSlice.Shared.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that validates messages using DataAnnotations and Sannr compile-time validators.
/// Throws ValidationException on failure so ExceptionHandlingMiddleware formats the RFC 7807 problem details with HTTP 400.
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
        if (message is not null && Sannr.SannrValidatorRegistry.TryGetValidator(typeof(TMessage), out var sannrValidator) && sannrValidator is not null)
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

            throw new ValidationException(validationErrors, "Validation failed.");
        }

        return await next(message, cancellationToken);
    }
}


