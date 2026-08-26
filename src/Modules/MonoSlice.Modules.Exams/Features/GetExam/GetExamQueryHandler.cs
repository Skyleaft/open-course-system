using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetExam;

public sealed class GetExamQueryHandler : IQueryHandler<GetExamQuery, ApiResponse<ExamFullDetailDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetExamQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamFullDetailDto>> Handle(
        GetExamQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), query.Id);
        }

        var isInstructor = _currentUser.IsAuthenticated &&
            (_currentUser.UserId == exam.InstructorId || _currentUser.Roles.Contains("Admin") || _currentUser.Roles.Contains("Instructor"));

        var allQuestions = new List<QuestionResultDto>();
        var sectionDtos = new List<QuizSectionDetailDto>();

        foreach (var sec in exam.Sections.OrderBy(s => s.OrderIndex))
        {
            var secQuestions = new List<QuestionResultDto>();

            if (sec.QuestionBank is not null)
            {
                var questions = sec.QuestionBank.Questions.OrderBy(q => q.OrderIndex).ToList();
                if (sec.QuestionCount.HasValue && sec.QuestionCount.Value > 0)
                {
                    questions = questions.Take(sec.QuestionCount.Value).ToList();
                }

                foreach (var q in questions)
                {
                    var qDto = new QuestionResultDto(
                        q.Id,
                        exam.Id,
                        sec.Id,
                        q.QuestionText,
                        q.Type.ToString(),
                        sec.PointsOverride ?? q.Points,
                        q.OrderIndex,
                        isInstructor ? q.Explanation : null,
                        sec.QuestionBank.Category,
                        sec.QuestionBank.Tags,
                        q.Options.Select(o => new QuestionOptionDto(
                            o.Id,
                            o.Text,
                            isInstructor && o.IsCorrect
                        )).ToList(),
                        sec.QuestionBank.CreatedBy,
                        sec.QuestionBank.UpdatedBy,
                        sec.QuestionBank.CreatedAtUtc
                    );

                    secQuestions.Add(qDto);
                    allQuestions.Add(qDto);
                }
            }

            sectionDtos.Add(new QuizSectionDetailDto(
                sec.Id,
                sec.ExamId,
                sec.QuestionBankId,
                sec.QuestionBank?.Title,
                sec.Title,
                sec.Description,
                sec.OrderIndex,
                sec.PointsOverride,
                sec.QuestionCount,
                secQuestions
            ));
        }

        var ruleConfigDto = new ExamRuleConfigDto(
            exam.RuleConfig.Name,
            exam.RuleConfig.CanTabSwitch,
            exam.RuleConfig.MaxTabSwitchesAllowed,
            exam.RuleConfig.RestrictClipboardAndMouse,
            exam.RuleConfig.ForceFullscreen,
            exam.RuleConfig.KeyboardDetection,
            exam.RuleConfig.RequireCamera,
            exam.RuleConfig.SnapshotIntervalSeconds,
            exam.RuleConfig.RequireMicrophone,
            exam.RuleConfig.MaxAllowedViolations,
            exam.RuleConfig.AutoDisqualifyOnExceed);

        var dto = new ExamFullDetailDto(
            exam.Id,
            exam.InstructorId,
            exam.Title,
            exam.Description,
            exam.ExamRuleId,
            ruleConfigDto,
            exam.DurationMinutes,
            exam.PassingScore,
            exam.MaxAttempts,
            exam.AvailableFromUtc,
            exam.AvailableToUtc,
            exam.IsPublished,
            exam.ShuffleQuestions,
            exam.ShuffleOptions,
            exam.CreatedBy,
            exam.UpdatedBy,
            exam.CreatedAtUtc,
            sectionDtos,
            allQuestions);

        return ApiResponse.Ok(dto);
    }
}
