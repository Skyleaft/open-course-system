using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestion;

public static class DeleteQuestionEndpoint
{
    public static void MapDeleteQuestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/questions/{questionId:guid}", async (Guid questionId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteQuestionCommand(questionId), cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("DeleteQuestion")
        .WithSummary("Deletes a quiz question from an examination.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
