using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ExamRules.ListExamRules;

public sealed class ListExamRulesQueryHandler : IQueryHandler<ListExamRulesQuery, ApiResponse<IReadOnlyList<ExamRuleDto>>>
{
    private readonly ExamsDbContext _dbContext;

    public ListExamRulesQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<ExamRuleDto>>> Handle(ListExamRulesQuery query, CancellationToken cancellationToken)
    {
        // Seed default presets if none exist yet
        if (!await _dbContext.ExamRules.AnyAsync(cancellationToken))
        {
            var presets = new List<ExamRule>
            {
                ExamRule.Create(
                    "Strict Proctored",
                    "Full anti-cheat with camera, microphone, fullscreen lock, and zero-tolerance tab switching.",
                    isSystemPreset: true,
                    canTabSwitch: false,
                    maxTabSwitchesAllowed: 0,
                    restrictClipboardAndMouse: true,
                    forceFullscreen: true,
                    keyboardDetection: true,
                    requireCamera: true,
                    snapshotIntervalSeconds: 45,
                    requireMicrophone: true,
                    maxAllowedViolations: 3,
                    autoDisqualifyOnExceed: true),

                ExamRule.Create(
                    "Standard Classroom Quiz",
                    "Fullscreen exam with keyboard & clipboard protection and limited tab switch grace attempts.",
                    isSystemPreset: true,
                    canTabSwitch: false,
                    maxTabSwitchesAllowed: 3,
                    restrictClipboardAndMouse: true,
                    forceFullscreen: true,
                    keyboardDetection: true,
                    requireCamera: false,
                    snapshotIntervalSeconds: 45,
                    requireMicrophone: false,
                    maxAllowedViolations: 3,
                    autoDisqualifyOnExceed: true),

                ExamRule.Create(
                    "Open Book / Coding Challenge",
                    "Permits tab switching and resource browsing while capturing occasional periodic camera snapshots.",
                    isSystemPreset: true,
                    canTabSwitch: true,
                    maxTabSwitchesAllowed: 999,
                    restrictClipboardAndMouse: false,
                    forceFullscreen: false,
                    keyboardDetection: false,
                    requireCamera: true,
                    snapshotIntervalSeconds: 60,
                    requireMicrophone: false,
                    maxAllowedViolations: 5,
                    autoDisqualifyOnExceed: false),

                ExamRule.Create(
                    "Practice / Simulation Mode",
                    "Completely open practice environment with no restrictions, proctoring, or disqualification.",
                    isSystemPreset: true,
                    canTabSwitch: true,
                    maxTabSwitchesAllowed: 999,
                    restrictClipboardAndMouse: false,
                    forceFullscreen: false,
                    keyboardDetection: false,
                    requireCamera: false,
                    snapshotIntervalSeconds: 60,
                    requireMicrophone: false,
                    maxAllowedViolations: 999,
                    autoDisqualifyOnExceed: false)
            };

            _dbContext.ExamRules.AddRange(presets);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var dbQuery = _dbContext.ExamRules.AsNoTracking();

        if (query.SystemPresetsOnly == true)
        {
            dbQuery = dbQuery.Where(r => r.IsSystemPreset);
        }

        var list = await dbQuery
            .OrderByDescending(r => r.IsSystemPreset)
            .ThenBy(r => r.Name)
            .Select(r => new ExamRuleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemPreset,
                r.CanTabSwitch,
                r.MaxTabSwitchesAllowed,
                r.RestrictClipboardAndMouse,
                r.ForceFullscreen,
                r.KeyboardDetection,
                r.RequireCamera,
                r.SnapshotIntervalSeconds,
                r.RequireMicrophone,
                r.MaxAllowedViolations,
                r.AutoDisqualifyOnExceed,
                r.CreatedBy,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse.Ok<IReadOnlyList<ExamRuleDto>>(list, "Exam rules retrieved successfully.");
    }
}
