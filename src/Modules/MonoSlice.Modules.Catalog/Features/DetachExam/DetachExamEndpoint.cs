using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.DetachExam;

public static class DetachExamEndpoint
{
    public static void MapDetachExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:guid}/exams/{examId:guid}", async (
                Guid id,
                Guid examId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new DetachExamCommand
                {
                    CourseId = id,
                    ExamId = examId
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("DetachExamFromCourse")
            .WithSummary("Detach an exam from a course curriculum (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
