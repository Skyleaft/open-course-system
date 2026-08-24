using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.CreateQuestionBank;

public sealed class CreateQuestionBankCommandHandler : ICommandHandler<CreateQuestionBankCommand, ApiResponse<Guid>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateQuestionBankCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<Guid>> Handle(CreateQuestionBankCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return ApiResponse.Fail<Guid>("Unauthorized.", 401);
        }

        var bank = QuestionBank.Create(
            createdBy: _currentUser.UserId.Value,
            title: command.Title,
            description: command.Description,
            category: command.Category,
            tags: command.Tags);

        await _dbContext.QuestionBanks.AddAsync(bank, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(bank.Id, "Question Bank package created successfully.", 201);
    }
}
