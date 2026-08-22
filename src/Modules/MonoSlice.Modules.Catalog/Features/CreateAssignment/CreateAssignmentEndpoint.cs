using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.CreateAssignment;

public static class CreateAssignmentEndpoint
{
    public static void MapCreateAssignmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/assignments", async (
                Guid id,
                CreateAssignmentRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new CreateAssignmentCommand
                {
                    CourseId = id,
                    Title = request.Title,
                    Instruction = request.Instruction,
                    DeadlineUtc = request.DeadlineUtc,
                    MaxScore = request.MaxScore
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("CreateCourseAssignment")
            .WithSummary("Create an assignment in a course (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record CreateAssignmentRequest(
    string Title,
    string Instruction,
    DateTime DeadlineUtc,
    decimal MaxScore = 100m);
