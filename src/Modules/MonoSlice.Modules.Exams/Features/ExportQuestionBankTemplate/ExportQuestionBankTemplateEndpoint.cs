using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Exams.Services;

namespace MonoSlice.Modules.Exams.Features.ExportQuestionBankTemplate;

public static class ExportQuestionBankTemplateEndpoint
{
    public static void MapExportQuestionBankTemplateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/v1/exams/question-banks/template
        endpoints.MapGet("/question-banks/template", (IWordQuestionBankService wordService) =>
            {
                var bytes = wordService.GenerateTemplateDocx();
                const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                const string fileName = "QuestionBank-Template.docx";

                return Results.File(bytes, contentType, fileName);
            })
            .WithName("ExportQuestionBankTemplate")
            .WithSummary("Export standard Word Document (.docx) Question Bank template for bulk import")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
