using System.Diagnostics;
using Mediator;
using OpenTelemetry.Trace;

namespace MonoSlice.Shared.Infrastructure.Behaviors;

public sealed class MetricAndTraceBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly ActivitySource ActivitySource = new("MonoSlice.Mediator");

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TMessage).Name;
        using var activity = ActivitySource.StartActivity($"Handle {requestName}", ActivityKind.Internal);

        activity?.SetTag("mediator.message_type", typeof(TMessage).FullName);
        activity?.SetTag("mediator.response_type", typeof(TResponse).FullName);

        try
        {
            var response = await next(message, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
