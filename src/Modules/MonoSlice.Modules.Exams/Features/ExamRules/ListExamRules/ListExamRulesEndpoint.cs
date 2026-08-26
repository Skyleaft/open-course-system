using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ExamRules.ListExamRules;

public static class ListExamRulesEndpoint
{
    public static void MapListExamRulesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("rules", async (
            [FromQuery] bool? systemPresetsOnly,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListExamRulesQuery { SystemPresetsOnly = systemPresetsOnly };
            var result = await mediator.Send(query, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("ListExamRules")
        .WithSummary("Lists system presets and custom exam rules.")
        .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin", "Proctor"));
    }
}
