using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ExamRules.DeleteExamRule;

public static class DeleteExamRuleEndpoint
{
    public static void MapDeleteExamRuleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("rules/{id:guid}", async (
            [FromRoute] Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteExamRuleCommand { Id = id };
            var result = await mediator.Send(command, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("DeleteExamRule")
        .WithSummary("Deletes a custom exam security rule policy.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
