using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestionBank;

public static class UpdateQuestionBankEndpoint
{
    public static void MapUpdateQuestionBankEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/question-banks/{id:guid}", async (
                Guid id,
                UpdateQuestionBankCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enriched = new UpdateQuestionBankCommand
                {
                    Id = id,
                    Title = command.Title,
                    Description = command.Description,
                    Category = command.Category,
                    Tags = command.Tags
                };
                var response = await mediator.Send(enriched, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateQuestionBank")
            .WithSummary("Update a Question Bank package metadata (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
