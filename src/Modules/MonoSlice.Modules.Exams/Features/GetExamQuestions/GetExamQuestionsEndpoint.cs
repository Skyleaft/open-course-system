using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetExamQuestions;

public static class GetExamQuestionsEndpoint
{
    public static void MapGetExamQuestionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/submissions/{submissionId:guid}/questions", async (
                Guid submissionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetExamQuestionsQuery(submissionId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetStudentExamQuestions")
            .WithSummary("Get student randomized exam questions with answers stripped")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
