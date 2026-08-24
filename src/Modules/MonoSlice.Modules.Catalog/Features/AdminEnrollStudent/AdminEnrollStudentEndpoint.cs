using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.AdminEnrollStudent;

public sealed record AdminEnrollStudentRequest(Guid? UserId, string? Email);

public static class AdminEnrollStudentEndpoint
{
    public static void MapAdminEnrollStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{courseId:guid}/enrollments", async (
            Guid courseId,
            AdminEnrollStudentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AdminEnrollStudentCommand(
                courseId,
                request.UserId,
                request.Email);

            var result = await mediator.Send(command, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("AdminEnrollStudent")
        .WithSummary("Manually enroll a student into a course (Instructor/Admin only)")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
