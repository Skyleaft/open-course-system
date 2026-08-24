using CoreLib.Common;
using CoreLib.TakeExam;
using Web.Client.Shared.Models;

namespace Web.Client.Interfaces;

public interface IUserExam
{
    public Task<CreatedResponse<UserExam>> Create(CreateUserExamDTO r, CancellationToken ct);
    public Task<ServiceResponse> Update(UserExam r, CancellationToken ct);
    public Task<ServiceResponse> Delete(Guid Id);
    public Task<UserExam> Get(Guid Id, CancellationToken ct);
    public Task<UserExam> GetOnly(Guid Id);
    public Task<IEnumerable<UserExam>> GetAll(Guid RoomId, CancellationToken ct);
    public Task<PaginatedResponse<UserExam>> Find(FindRequest r, CancellationToken ct, string? UserId = "");
    public Task<ServiceResponse> UpdateJawaban(UpdateJawabanDTO r, CancellationToken ct);
    public Task<List<UserAnswer>> GetUserAnswers(Guid UserExamId, CancellationToken ct);
    public Task<ServiceResponse> RetryExam(Guid UserExamId, CancellationToken ct);
    public Task<ServiceResponse> StartExam(Guid UserExamId, CancellationToken ct);
}

public class CreateUserExamDTO
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public bool IsOngoing { get; set; }
    public bool IsDone { get; set; }
    public int? RetryCount { get; set; }
    public Guid RoomId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeSpan TimeLeft { get; set; }
    public bool isExpired { get; set; }
    public List<UserAnswer>? UserAnswers { get; set; }
}

public class UpdateJawabanDTO
{
    public Guid UserAnswerId { get; set; }
    public Guid UserExamId { get; set; }
    public Guid? SoalJawabanId { get; set; }
    public TimeSpan TimeLeft { get; set; }
}