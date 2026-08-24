using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.CreateQuestionBank;

public sealed partial class CreateQuestionBankCommand : ICommand<ApiResponse<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
}
