using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.UpdateExam;

public static class UpdateExamEndpoint
{
    public static void MapUpdateExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{id:guid}", async (
                Guid id,
                UpdateExamCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enrichedCommand = new UpdateExamCommand
                {
                    Id = id,
                    Title = command.Title,
                    Description = command.Description,
                    Mode = command.Mode,
                    DurationMinutes = command.DurationMinutes,
                    PassingScore = command.PassingScore,
                    MaxAllowedViolations = command.MaxAllowedViolations,
                    MaxAttempts = command.MaxAttempts,
                    AvailableFromUtc = command.AvailableFromUtc,
                    AvailableToUtc = command.AvailableToUtc,
                    ShuffleQuestions = command.ShuffleQuestions,
                    ShuffleOptions = command.ShuffleOptions
                };
                var response = await mediator.Send(enrichedCommand, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateExam")
            .WithSummary("Update exam parameters (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
