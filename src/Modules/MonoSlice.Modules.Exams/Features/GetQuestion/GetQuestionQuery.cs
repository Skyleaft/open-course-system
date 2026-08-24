using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetQuestion;

public sealed record GetQuestionQuery(Guid QuestionId) : IQuery<ApiResponse<QuestionResultDto>>;
