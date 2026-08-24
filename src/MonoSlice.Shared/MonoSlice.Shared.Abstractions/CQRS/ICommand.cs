using Mediator;

namespace MonoSlice.Shared.Abstractions.CQRS;

/// <summary>
/// Marker interface for commands that return a response.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;

/// <summary>
/// Marker interface for commands with no return value.
/// </summary>
public interface ICommand : IRequest;
