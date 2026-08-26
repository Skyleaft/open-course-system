using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.BroadcastExamMessage;

public sealed record BroadcastExamMessageRequest(string Message);

public static class BroadcastExamMessageEndpoint
{
    public static void MapBroadcastExamMessageEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/exams/{examId:guid}/broadcast", async (
                Guid examId,
                [FromBody] BroadcastExamMessageRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new BroadcastExamMessageCommand(examId, request.Message);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("BroadcastExamMessage")
            .WithSummary("Send real-time announcement broadcast to all candidates in an exam room")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin", "Proctor"));
    }
}

