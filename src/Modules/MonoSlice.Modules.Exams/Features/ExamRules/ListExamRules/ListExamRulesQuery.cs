using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ExamRules.ListExamRules;

public sealed partial class ListExamRulesQuery : IQuery<ApiResponse<IReadOnlyList<ExamRuleDto>>>
{
    public bool? SystemPresetsOnly { get; init; }
}
