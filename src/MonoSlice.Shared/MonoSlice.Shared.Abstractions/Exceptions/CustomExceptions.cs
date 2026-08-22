namespace MonoSlice.Shared.Abstractions.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.") { }
}

/// <summary>
/// Thrown when a validation rule is violated.
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string error)
        : base(error)
    {
        Errors = [error];
    }
}

/// <summary>
/// Thrown when a business rule is violated.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a user is not authorized to perform an action.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string? message = null)
        : base(message ?? "You are not authorized to perform this action.") { }
}
