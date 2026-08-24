using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public static class AddQuestionEndpoint
{
    public static void MapAddQuestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/questions", async (
                AddQuestionCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddQuestionToPool")
            .WithSummary("Add a question to Question Bank repository (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));

        endpoints.MapPost("/question-banks/{bankId:guid}/questions", async (
                Guid bankId,
                AddQuestionCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enriched = new AddQuestionCommand
                {
                    BankId = bankId,
                    QuestionText = command.QuestionText,
                    Type = command.Type,
                    Points = command.Points,
                    Explanation = command.Explanation,
                    Category = command.Category,
                    Tags = command.Tags,
                    Options = command.Options
                };
                var response = await mediator.Send(enriched, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddQuestionToBank")
            .WithSummary("Add a question directly to a specific Question Bank package (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));

        endpoints.MapPost("/{id:guid}/questions", async (
                Guid id,
                AddQuestionCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enriched = new AddQuestionCommand
                {
                    ExamId = id,
                    QuestionText = command.QuestionText,
                    Type = command.Type,
                    Points = command.Points,
                    Explanation = command.Explanation,
                    Category = command.Category,
                    Tags = command.Tags,
                    Options = command.Options
                };
                var response = await mediator.Send(enriched, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddExamQuestion")
            .WithSummary("Add a question to an exam section's question pool (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
