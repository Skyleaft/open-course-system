using Mediator;

namespace MonoSlice.Shared.Abstractions.CQRS;

/// <summary>
/// Handler for commands that return a response.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull;

/// <summary>
/// Handler for commands with no return value.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;
