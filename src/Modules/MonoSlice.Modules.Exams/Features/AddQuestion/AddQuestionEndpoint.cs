using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public static class AddQuestionEndpoint
{
    public static void MapAddQuestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/questions", async (
                Guid id,
                AddQuestionCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enriched = command with { ExamId = id };
                var response = await mediator.Send(enriched, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddExamQuestion")
            .WithSummary("Add a question with answer keys to an exam (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
