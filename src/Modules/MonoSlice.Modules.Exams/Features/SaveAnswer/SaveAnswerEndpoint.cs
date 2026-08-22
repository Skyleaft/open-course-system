using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.SaveAnswer;

public static class SaveAnswerEndpoint
{
    public static void MapSaveAnswerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/answers", async (
                Guid submissionId,
                SaveAnswerRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new SaveAnswerCommand
                {
                    SubmissionId = submissionId,
                    QuestionId = request.QuestionId,
                    SelectedOptionIds = request.SelectedOptionIds,
                    EssayText = request.EssayText
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("SaveStudentExamAnswer")
            .WithSummary("Save or update student answer for a specific question")
            .RequireAuthorization();
    }
}

public sealed record SaveAnswerRequest(
    Guid QuestionId,
    List<Guid>? SelectedOptionIds,
    string? EssayText);
