using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.CreateExam;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.UpdateExam;

public sealed class UpdateExamCommandHandler : ICommandHandler<UpdateExamCommand, ApiResponse<ExamDetailDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateExamCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamDetailDto>> Handle(
        UpdateExamCommand command,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .Include(e => e.Sections)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.Id);
        }

        // Rule: cannot update if there are active InProgress submissions
        var hasActiveSubmissions = await _dbContext.Submissions
            .AnyAsync(s => s.ExamId == command.Id && s.Status == SubmissionStatus.InProgress, cancellationToken);

        if (hasActiveSubmissions)
        {
            throw new BusinessRuleException("Cannot modify exam parameters while students have active in-progress attempts.");
        }

        exam.Update(
            command.Title,
            command.Description,
            command.Mode,
            command.DurationMinutes,
            command.PassingScore,
            command.MaxAllowedViolations,
            command.MaxAttempts,
            command.AvailableFromUtc,
            command.AvailableToUtc,
            command.ShuffleQuestions,
            command.ShuffleOptions,
            updatedBy: _currentUser.UserId);

        // Synchronize Sections if provided
        if (command.Sections is not null)
        {
            var requestedSectionIds = command.Sections
                .Where(s => s.Id.HasValue && s.Id.Value != Guid.Empty)
                .Select(s => s.Id!.Value)
                .ToHashSet();

            // 1. Remove sections not present in request
            var sectionsToRemove = exam.Sections
                .Where(s => !requestedSectionIds.Contains(s.Id))
                .ToList();

            foreach (var sec in sectionsToRemove)
            {
                exam.RemoveSection(sec.Id);
                _dbContext.Sections.Remove(sec);
            }

            // 2. Update existing or add new
            foreach (var secDto in command.Sections)
            {
                if (secDto.Id.HasValue && secDto.Id.Value != Guid.Empty)
                {
                    var existing = exam.Sections.FirstOrDefault(s => s.Id == secDto.Id.Value);
                    if (existing is not null)
                    {
                        existing.Update(
                            questionBankId: secDto.QuestionBankId,
                            title: secDto.Title,
                            orderIndex: secDto.OrderIndex,
                            pointsOverride: secDto.PointsOverride,
                            questionCount: secDto.QuestionCount,
                            description: secDto.Description);
                    }
                    else
                    {
                        exam.AddSection(
                            questionBankId: secDto.QuestionBankId,
                            title: secDto.Title,
                            pointsOverride: secDto.PointsOverride,
                            questionCount: secDto.QuestionCount,
                            description: secDto.Description);
                    }
                }
                else
                {
                    exam.AddSection(
                        questionBankId: secDto.QuestionBankId,
                        title: secDto.Title,
                        pointsOverride: secDto.PointsOverride,
                        questionCount: secDto.QuestionCount,
                        description: secDto.Description);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = exam.Adapt<ExamDetailDto>() with
        {
            Mode = exam.Mode.ToString()
        };

        return ApiResponse.Ok(dto, "Exam updated successfully.");
    }
}
