using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GradeEssaySubmission;

public static class GradeEssaySubmissionEndpoint
{
    public static void MapGradeEssaySubmissionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/grade", async (
                Guid submissionId,
                GradeEssaySubmissionRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new GradeEssaySubmissionCommand(submissionId, request.Grades ?? []);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GradeEssaySubmission")
            .WithSummary("Grading essay responses and recalculating candidate exam score (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record GradeEssaySubmissionRequest(List<EssayQuestionGradeDto> Grades);
