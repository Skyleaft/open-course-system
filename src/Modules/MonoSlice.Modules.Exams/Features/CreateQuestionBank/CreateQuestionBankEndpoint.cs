using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.CreateQuestionBank;

public static class CreateQuestionBankEndpoint
{
    public static void MapCreateQuestionBankEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/question-banks", async (
                CreateQuestionBankCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("CreateQuestionBank")
            .WithSummary("Create a new Question Bank package pool (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
