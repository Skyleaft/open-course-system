using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.StartExam;

public static class StartExamEndpoint
{
    public static void MapStartExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/start", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new StartExamCommand { ExamId = id };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("StartExamAttempt")
            .WithSummary("Start an attempt on an exam and retrieve active session token")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
