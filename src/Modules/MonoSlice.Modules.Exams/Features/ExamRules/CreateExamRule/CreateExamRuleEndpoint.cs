using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ExamRules.CreateExamRule;

public static class CreateExamRuleEndpoint
{
    public static void MapCreateExamRuleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("rules", async (
            [FromBody] CreateExamRuleCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("CreateExamRule")
        .WithSummary("Creates a new custom exam rule policy")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
