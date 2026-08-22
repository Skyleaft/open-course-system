using Mediator;

namespace MonoSlice.Shared.Abstractions.CQRS;

/// <summary>
/// Marker interface for queries that return a response.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
