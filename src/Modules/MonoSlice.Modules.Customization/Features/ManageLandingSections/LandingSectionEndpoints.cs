using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.ManageLandingSections;

public static class LandingSectionEndpoints
{
    public static void MapLandingSectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/landing-sections")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetLandingSectionsQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetLandingSections")
        .WithSummary("List all landing page sections.");

        group.MapPost("/", async (
            CreateLandingSectionRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateLandingSectionCommand(
                request.SectionType,
                request.Title,
                request.Subtitle,
                request.OrderIndex,
                request.IsActive,
                request.ConfigJson);

            var result = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/v1/customization/admin/landing-sections/{result.Data}", result);
        })
        .WithName("CreateLandingSection")
        .WithSummary("Create a new landing page section.");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateLandingSectionRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateLandingSectionCommand(
                id,
                request.Title,
                request.Subtitle,
                request.OrderIndex,
                request.IsActive,
                request.ConfigJson);

            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpdateLandingSection")
        .WithSummary("Update an existing landing page section.");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteLandingSectionCommand(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("DeleteLandingSection")
        .WithSummary("Delete a landing page section.");

        group.MapPut("/reorder", async (
            ReorderLandingSectionsRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReorderLandingSectionsCommand(request.SectionIds), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ReorderLandingSections")
        .WithSummary("Reorder landing page sections.");
    }
}

public sealed record CreateLandingSectionRequest(
    string SectionType,
    string? Title,
    string? Subtitle,
    int OrderIndex,
    bool IsActive,
    string ConfigJson);

public sealed record UpdateLandingSectionRequest(
    string? Title,
    string? Subtitle,
    int OrderIndex,
    bool IsActive,
    string ConfigJson);

public sealed record ReorderLandingSectionsRequest(List<Guid> SectionIds);
