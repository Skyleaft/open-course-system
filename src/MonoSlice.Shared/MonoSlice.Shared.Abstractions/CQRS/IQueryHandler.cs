using Mediator;

namespace MonoSlice.Shared.Abstractions.CQRS;

/// <summary>
/// Handler for queries that return a response.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull;
