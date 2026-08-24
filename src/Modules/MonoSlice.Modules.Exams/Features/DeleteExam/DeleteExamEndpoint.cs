using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.DeleteExam;

public static class DeleteExamEndpoint
{
    public static void MapDeleteExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteExamCommand(id), cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("DeleteExam")
        .WithSummary("Deletes an examination, cascading questions and submissions, and emits an asynchronous cleanup event.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
