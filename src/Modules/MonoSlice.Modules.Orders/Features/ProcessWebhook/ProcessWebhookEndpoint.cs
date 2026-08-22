using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Orders.Features.ProcessWebhook;

public static class ProcessWebhookEndpoint
{
    public static void MapProcessWebhookEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhook", async (
                HttpContext context,
                ProcessWebhookCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var signatureHeader = context.Request.Headers["X-Signature"].FirstOrDefault() ??
                                      context.Request.Headers["X-Webhook-Signature"].FirstOrDefault();

                var enrichedCommand = command with
                {
                    Signature = !string.IsNullOrWhiteSpace(command.Signature) ? command.Signature : signatureHeader
                };

                var response = await mediator.Send(enrichedCommand, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("ProcessPaymentWebhook")
            .WithSummary("Handle payment gateway webhook callback")
            .AllowAnonymous();
    }
}
