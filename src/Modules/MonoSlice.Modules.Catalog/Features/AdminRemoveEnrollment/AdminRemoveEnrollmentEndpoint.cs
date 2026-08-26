using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;

public static class AdminRemoveEnrollmentEndpoint
{
    public static void MapAdminRemoveEnrollmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{courseId:guid}/enrollments/{enrollmentId:guid}", async (
            Guid courseId,
            Guid enrollmentId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AdminRemoveEnrollmentCommand(courseId, enrollmentId);

            var result = await mediator.Send(command, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("AdminRemoveEnrollment")
        .WithSummary("Un-enroll / remove a student from a course (Instructor/Admin only)")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
