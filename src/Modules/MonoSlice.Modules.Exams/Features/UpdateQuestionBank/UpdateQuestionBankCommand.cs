using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestionBank;

public sealed partial class UpdateQuestionBankCommand : ICommand<ApiResponse<bool>>
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
}
