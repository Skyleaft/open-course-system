using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetQuestionBank;

public static class GetQuestionBankEndpoint
{
    public static void MapGetQuestionBankEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/question-banks/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(new GetQuestionBankQuery(id), ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetQuestionBank")
            .WithSummary("Get a Question Bank package and its questions (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
