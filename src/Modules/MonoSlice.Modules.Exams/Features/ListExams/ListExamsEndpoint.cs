using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ListExams;

public static class ListExamsEndpoint
{
    public static void MapListExamsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", async (
            [FromQuery] Guid? examRuleId,
            [FromQuery] string? ruleName,
            [FromQuery] bool? isPublished,
            [FromQuery] string? search,
            [FromQuery] int pageIndex,
            [FromQuery] int pageSize,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListExamsQuery(
                examRuleId,
                ruleName,
                isPublished,
                search,
                pageIndex <= 0 ? 1 : pageIndex,
                pageSize <= 0 ? 20 : pageSize);

            var result = await mediator.Send(query, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("ListExams")
        .WithSummary("Lists examinations with advanced filtering and pagination.")
        .AllowAnonymous();
    }
}
