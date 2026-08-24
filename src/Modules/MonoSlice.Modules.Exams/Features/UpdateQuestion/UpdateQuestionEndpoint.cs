using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestion;

public static class UpdateQuestionEndpoint
{
    public static void MapUpdateQuestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/questions/{questionId:guid}", async (Guid questionId, UpdateQuestionCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var commandWithId = new UpdateQuestionCommand
            {
                QuestionId = questionId,
                QuestionText = command.QuestionText,
                Type = command.Type,
                Points = command.Points,
                Explanation = command.Explanation,
                Options = command.Options
            };

            var result = await mediator.Send(commandWithId, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("UpdateQuestion")
        .WithSummary("Updates an existing quiz question, points, and option choices.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
