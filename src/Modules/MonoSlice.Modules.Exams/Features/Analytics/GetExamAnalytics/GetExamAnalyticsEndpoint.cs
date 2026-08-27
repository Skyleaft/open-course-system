using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetExamAnalytics;

public static class GetExamAnalyticsEndpoint
{
    public static void MapGetExamAnalyticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/instructor/exams/{examId:guid}/analytics", async (
                Guid examId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetExamAnalyticsQuery { ExamId = examId };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetInstructorExamAnalytics")
            .WithSummary("Get detailed exam statistics, score distributions, and psychometric item analysis")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
