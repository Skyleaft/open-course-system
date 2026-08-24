using CoreLib.BankSoal;
using CoreLib.Common;
using Web.Client.Shared.Models;

namespace Web.Client.Interfaces;

public interface IExam
{
    public Task<CreatedResponse<Exam>> Create(Exam r, CancellationToken ct);
    public Task<ServiceResponse> Update(Exam r, CancellationToken ct);
    public Task<ServiceResponse> Delete(int Id);
    public Task<Exam> Get(int Id);
    public Task<PaginatedResponse<Exam>> Find(FindRequest r, CancellationToken ct);
}