using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ExamRules.GetExamRule;

public static class GetExamRuleEndpoint
{
    public static void MapGetExamRuleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("rules/{id:guid}", async (
            [FromRoute] Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExamRuleQuery { Id = id };
            var result = await mediator.Send(query, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("GetExamRule")
        .WithSummary("Retrieves an exam rule by ID.")
        .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin", "Proctor"));
    }
}
