using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GrantRetake;

public record GrantRetakeRequest(string? Reason = null);

public static class GrantExamRetakeEndpoint
{
    public static void MapGrantExamRetakeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{examId:guid}/students/{studentId:guid}/retake", async (
                Guid examId,
                Guid studentId,
                GrantRetakeRequest? req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new GrantExamRetakeCommand
                {
                    ExamId = examId,
                    StudentId = studentId,
                    Reason = req?.Reason
                };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GrantExamRetake")
            .WithSummary("Grants a student permission to retake an examination by resetting/unlocking their attempt")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
