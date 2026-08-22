using CoreLib.BankSoal;
using CoreLib.Common;
using Microsoft.EntityFrameworkCore;
using Web.Client.Shared.Models;
using Web.Common.Database;

namespace Web.Services.ExamServices;

public class ExamService : IExam
{
    private readonly AppDbContext _dbContext;

    public ExamService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreatedResponse<Exam>> Create(Exam r, CancellationToken ct)
    {
        r.CreatedOn = DateTime.UtcNow;
        var created = _dbContext.Exam.Add(r);
        await _dbContext.SaveChangesAsync(ct);
        return new CreatedResponse<Exam>(true, "Data Berhasil Ditambahkan", created.Entity);
    }

    public async Task<ServiceResponse> Update(Exam r, CancellationToken ct)
    {
        var exam = _dbContext
            .Exam
            .Include(x => x.Soals!.OrderBy(s => s.Nomor))
            .ThenInclude(xs => xs.PilihanJawaban)
            .FirstOrDefault(x => x.Id == r.Id);
        if (exam == null) return new ServiceResponse(false, "data tidak ditemukan");

        r.LastModifiedOn = DateTime.UtcNow;
        var entry = _dbContext.Entry(exam);
        entry.CurrentValues.SetValues(r);

        var deletedSoal = entry.Entity.Soals.Except(r.Soals).ToList();
        var addedSoal = r.Soals.Except(entry.Entity.Soals).ToList();


        _dbContext.Soal.RemoveRange(deletedSoal);
        _dbContext.Soal.AddRange(addedSoal);

        await _dbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "data berhasil diupdate");
    }

    public async Task<ServiceResponse> Delete(int Id)
    {
        var find = _dbContext.Exam.Find(Id);
        if (find == null) return new ServiceResponse(false, "data tidak ditemukan");

        _dbContext.Exam.Remove(find);
        await _dbContext.SaveChangesAsync();
        return new ServiceResponse(true, "data berhasil dihapus");
    }

    public async Task<Exam> Get(int Id)
    {
        var find = _dbContext
            .Exam
            .Include(x => x.Soals.OrderBy(s => s.Nomor))
            .ThenInclude(xs => xs.PilihanJawaban)
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == Id);
        if (find == null) return null;

        return find;
    }

    public async Task<PaginatedResponse<Exam>> Find(FindRequest r, CancellationToken ct)
    {
        var data = await _dbContext.Exam.WhereIf(!string.IsNullOrEmpty(r.Search),
                x => x.Nama.ToLower()
                    .Contains(r.Search.ToLower()))
            .OrderByDescending(x => x.CreatedOn)
            .ToPaginatedList(r.Page, r.PageSize, r.OrderBy, r.Direction, ct);
        return data;
    }
}