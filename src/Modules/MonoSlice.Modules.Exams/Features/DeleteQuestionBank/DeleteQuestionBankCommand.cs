using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestionBank;

public sealed partial class DeleteQuestionBankCommand : ICommand<ApiResponse<bool>>
{
    public Guid Id { get; init; }

    public DeleteQuestionBankCommand() { }

    public DeleteQuestionBankCommand(Guid id)
    {
        Id = id;
    }
}
