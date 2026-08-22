using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetDeadLetters;

public sealed class GetDeadLettersQueryHandler : IQueryHandler<GetDeadLettersQuery, ApiResponse<IReadOnlyList<DeadLetterDto>>>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetDeadLettersQueryHandler(
        AssessmentsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<DeadLetterDto>>> Handle(
        GetDeadLettersQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var dbQuery = _dbContext.GradingDeadLetters.AsNoTracking();

        if (query.OnlyUnresolved == true)
        {
            dbQuery = dbQuery.Where(d => !d.IsResolved);
        }

        var list = await dbQuery
            .OrderByDescending(d => d.FailedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(d => d.Adapt<DeadLetterDto>()).ToList();

        return ApiResponse.Ok<IReadOnlyList<DeadLetterDto>>(dtos);
    }
}
