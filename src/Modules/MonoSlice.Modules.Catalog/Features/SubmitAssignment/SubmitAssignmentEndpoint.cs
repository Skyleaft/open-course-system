using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.SubmitAssignment;

public static class SubmitAssignmentEndpoint
{
    public static void MapSubmitAssignmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/assignments/{assignmentId:guid}/submit", async (
                Guid assignmentId,
                SubmitAssignmentRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new SubmitAssignmentCommand
                {
                    AssignmentId = assignmentId,
                    FileUrl = request.FileUrl
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("SubmitAssignment")
            .WithSummary("Submit assignment solution file before deadline")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}

public sealed record SubmitAssignmentRequest(string FileUrl);
