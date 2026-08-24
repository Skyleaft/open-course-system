using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Exams.Domain;

namespace MonoSlice.Modules.Exams.Features.ListQuestions;

public static class ListQuestionsEndpoint
{
    public static void MapListQuestionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/questions", async (
                Guid? bankId,
                string? search,
                QuestionType? type,
                string? category,
                int? pageIndex,
                int? pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new ListQuestionsQuery
                {
                    BankId = bankId,
                    SearchTerm = search,
                    Type = type,
                    Category = category,
                    PageIndex = pageIndex ?? 1,
                    PageSize = pageSize ?? 50
                };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("ListBankQuestions")
            .WithSummary("List all questions across Question Bank pools with filters (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
