using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.AttachExam;

public static class AttachExamEndpoint
{
    public static void MapAttachExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/exams", async (
                Guid id,
                AttachExamRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new AttachExamCommand
                {
                    CourseId = id,
                    ExamId = request.ExamId,
                    OrderIndex = request.OrderIndex ?? 1,
                    IsMandatory = request.IsMandatory ?? true
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AttachExamToCourse")
            .WithSummary("Attach an exam to a course curriculum (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record AttachExamRequest(Guid ExamId, int? OrderIndex, bool? IsMandatory);
