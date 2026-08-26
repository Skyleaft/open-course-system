using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.ExamRules.DeleteExamRule;

public sealed partial class DeleteExamRuleCommand : ICommand<ApiResponse<bool>>
{
    [Required]
    public Guid Id { get; init; }
}
