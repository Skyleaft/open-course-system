using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetQuestion;

public static class GetQuestionEndpoint
{
    public static void MapGetQuestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/questions/{questionId:guid}", async (Guid questionId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetQuestionQuery(questionId), cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("GetQuestion")
        .WithSummary("Retrieves a single quiz question with options and metadata.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin","Proctor"));
    }
}
