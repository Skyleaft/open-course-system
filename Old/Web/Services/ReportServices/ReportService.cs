using CoreLib.Common;
using CoreLib.Report;
using Microsoft.EntityFrameworkCore;
using Web.Common.Database;

namespace Web.Services.ReportServices;

public class ReportService(AppDbContext _dbContext) : IReport
{
    public async Task<ExamReport> Get(Guid Id, CancellationToken ct)
    {
        var find = _dbContext
            .ExamReport
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == Id);
        if (find == null) return null;

        return find;
    }

    public async Task<PaginatedResponse<ExamReport>> Find(FindRequest r, CancellationToken ct)
    {
        var data = await _dbContext
            .ExamReport
            .WhereIf(!string.IsNullOrEmpty(r.Search),
                x => x.NamaLengkap.ToLower().Contains(r.Search.ToLower()) ||
                     x.AsalKota.ToLower().Contains(r.Search.ToLower()) ||
                     x.NamaRoom.ToLower().Contains(r.Search.ToLower())
            )
            .Where(x => x.NamaRoom.Contains(r.Filter))
            .OrderBy(x => x.Jadwal)
            .ToPaginatedList(r.Page, r.PageSize, r.OrderBy, r.Direction, ct);
        return data;
    }
}