using CoreLib.Common;
using CoreLib.Report;

namespace Web.Client.Interfaces;

public interface IReport
{
    public Task<ExamReport> Get(Guid Id, CancellationToken ct);
    public Task<PaginatedResponse<ExamReport>> Find(FindRequest r, CancellationToken ct);
}