using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetExamSubmissions;

public static class GetExamSubmissionsEndpoint
{
    public static void MapGetExamSubmissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:guid}/submissions", async (
                Guid id,
                Guid? studentId,
                string? status,
                int? page,
                int? pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetExamSubmissionsQuery
                {
                    ExamId = id,
                    StudentId = studentId,
                    Status = status,
                    PageIndex = page ?? 1,
                    PageSize = pageSize ?? 20
                };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetExamSubmissions")
            .WithSummary("Retrieves paginated exam submissions and candidate performance details for instructors and admins")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
