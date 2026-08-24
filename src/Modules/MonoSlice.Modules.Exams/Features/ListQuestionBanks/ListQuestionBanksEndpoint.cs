using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ListQuestionBanks;

public static class ListQuestionBanksEndpoint
{
    public static void MapListQuestionBanksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/question-banks", async (
                string? search,
                string? category,
                int? pageIndex,
                int? pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new ListQuestionBanksQuery
                {
                    SearchTerm = search,
                    Category = category,
                    PageIndex = pageIndex ?? 1,
                    PageSize = pageSize ?? 50
                };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("ListQuestionBanks")
            .WithSummary("List Question Bank packages with optional category/search filters (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
