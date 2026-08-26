using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.ExamRules.UpdateExamRule;

public static class UpdateExamRuleEndpoint
{
    public static void MapUpdateExamRuleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("rules/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateExamRuleCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (id != command.Id)
            {
                command = new UpdateExamRuleCommand
                {
                    Id = id,
                    Name = command.Name,
                    Description = command.Description,
                    CanTabSwitch = command.CanTabSwitch,
                    MaxTabSwitchesAllowed = command.MaxTabSwitchesAllowed,
                    RestrictClipboardAndMouse = command.RestrictClipboardAndMouse,
                    ForceFullscreen = command.ForceFullscreen,
                    KeyboardDetection = command.KeyboardDetection,
                    RequireCamera = command.RequireCamera,
                    SnapshotIntervalSeconds = command.SnapshotIntervalSeconds,
                    RequireMicrophone = command.RequireMicrophone,
                    MaxAllowedViolations = command.MaxAllowedViolations,
                    AutoDisqualifyOnExceed = command.AutoDisqualifyOnExceed
                };
            }

            var result = await mediator.Send(command, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("UpdateExamRule")
        .WithSummary("Updates an existing exam security rule policy.")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
