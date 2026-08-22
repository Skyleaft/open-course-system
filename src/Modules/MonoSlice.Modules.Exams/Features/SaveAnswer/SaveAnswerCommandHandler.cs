using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.SaveAnswer;

public sealed class SaveAnswerCommandHandler : ICommandHandler<SaveAnswerCommand, ApiResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public SaveAnswerCommandHandler(
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse> Handle(
        SaveAnswerCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var redisKey = $"exam_answers:{command.SubmissionId}";

        // Retrieve existing buffered answers from Redis cache
        var answers = await _cacheService.GetAsync<Dictionary<Guid, CachedAnswerDto>>(redisKey, cancellationToken)
                      ?? new Dictionary<Guid, CachedAnswerDto>();

        answers[command.QuestionId] = new CachedAnswerDto(
            command.QuestionId,
            command.SelectedOptionIds,
            command.EssayText,
            DateTime.UtcNow);

        // Store back in Redis with a 4-hour buffer TTL without hitting PostgreSQL
        await _cacheService.SetAsync(redisKey, answers, TimeSpan.FromHours(4), cancellationToken);

        return ApiResponse.Ok("Answer buffered in Redis cache successfully.");
    }
}
