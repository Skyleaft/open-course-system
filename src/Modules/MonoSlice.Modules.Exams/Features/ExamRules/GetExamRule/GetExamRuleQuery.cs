using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.ExamRules.GetExamRule;

public sealed partial class GetExamRuleQuery : IQuery<ApiResponse<ExamRuleDto>>
{
    [Required]
    public Guid Id { get; init; }
}
