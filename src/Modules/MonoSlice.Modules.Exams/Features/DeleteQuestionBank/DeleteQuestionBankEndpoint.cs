using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestionBank;

public static class DeleteQuestionBankEndpoint
{
    public static void MapDeleteQuestionBankEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/question-banks/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(new DeleteQuestionBankCommand(id), ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("DeleteQuestionBank")
            .WithSummary("Delete a Question Bank package (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
