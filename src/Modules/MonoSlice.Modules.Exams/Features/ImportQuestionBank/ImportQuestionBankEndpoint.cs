using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Exams.Features.ImportQuestionBank;

public static class ImportQuestionBankEndpoint
{
    public static void MapImportQuestionBankEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/exams/question-banks/import (New Bank)
        endpoints.MapPost("/question-banks/import", async (
                [FromForm] IFormFile file,
                [FromForm] string? title,
                [FromForm] string? description,
                [FromForm] string? category,
                [FromForm] string? tags,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(ApiResponse.Fail<ImportQuestionBankResultDto>("A valid Word Document (.docx) file is required.", 400));
                }

                var tagList = !string.IsNullOrWhiteSpace(tags)
                    ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : new List<string>();

                using var stream = file.OpenReadStream();
                var command = new ImportQuestionBankCommand
                {
                    FileStream = stream,
                    FileName = file.FileName,
                    Title = title,
                    Description = description,
                    Category = category,
                    Tags = tagList
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .DisableAntiforgery()
            .WithName("ImportQuestionBank")
            .WithSummary("Import questions from Word Document (.docx) into a new Question Bank (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));

        // POST /api/v1/exams/question-banks/{id}/import (Append to existing bank)
        endpoints.MapPost("/question-banks/{id:guid}/import", async (
                Guid id,
                [FromForm] IFormFile file,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(ApiResponse.Fail<ImportQuestionBankResultDto>("A valid Word Document (.docx) file is required.", 400));
                }

                using var stream = file.OpenReadStream();
                var command = new ImportQuestionBankCommand
                {
                    TargetBankId = id,
                    FileStream = stream,
                    FileName = file.FileName
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .DisableAntiforgery()
            .WithName("ImportQuestionsIntoBank")
            .WithSummary("Import and append questions from Word Document (.docx) into an existing Question Bank (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
