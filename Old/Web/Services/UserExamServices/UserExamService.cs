using System.Security.Claims;
using CoreLib.Common;
using CoreLib.TakeExam;
using Mapster;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Web.Client.Shared.Models;
using Web.Common.Database;

namespace Web.Services.UserExamServices;

public class UserExamService : IUserExam
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly AppDbContext _dbContext;

    public UserExamService(AuthenticationStateProvider authenticationStateProvider, AppDbContext dbContext)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _dbContext = dbContext;
    }

    public async Task<CreatedResponse<UserExam>> Create(CreateUserExamDTO r, CancellationToken ct)
    {
        var check = await _dbContext.UserExam.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == r.UserId && x.RoomId == r.RoomId, ct);
        if (check != null)
            return new CreatedResponse<UserExam>(false,
                "Anda Sudah Berada Diruangan Tersebut, Silahkan Lihat di Ruangan Ujian");
        var data = r.Adapt<UserExam>();
        if (r.isExpired)
        {
            data.IsDone = true;
            data.RetryCount = 0;
        }

        var created = await _dbContext.UserExam.AddAsync(data, ct);
        await _dbContext.SaveChangesAsync(ct);
        return new CreatedResponse<UserExam>(true, "Data Berhasil Ditambahkan", created.Entity);
    }

    public async Task<ServiceResponse> Update(UserExam r, CancellationToken ct)
    {
        var userExam = await Get(r.Id, ct);
        if (userExam == null) return new ServiceResponse(false, "data tidak ditemukan");

        var nomalizeScore = r.CalculateScoreNormalize;

        var entry = _dbContext.Entry(userExam);
        entry.Entity.EndDate = r.EndDate;
        entry.Entity.StartDate = r.StartDate;
        entry.Entity.RoomId = r.RoomId;
        entry.Entity.UserId = r.UserId;
        entry.Entity.IsDone = r.IsDone;
        entry.Entity.IsOngoing = r.IsOngoing;
        entry.Entity.TimeLeft = r.TimeLeft;
        entry.Entity.RetryCount = r.RetryCount;
        entry.Entity.ScoreData = r.CalculateScore;
        entry.Entity.ScoreNormalizeData = nomalizeScore;
        if (entry.Entity.HistoryScoreNormalize == null) entry.Entity.HistoryScoreNormalize = new List<double>();

        entry.Entity.HistoryScoreNormalize.Add((double)nomalizeScore);

        // if (r.UserAnswers != null)
        // {
        //     var existingAnswers = userExam.UserAnswers.ToList();
        //     var updatedAnswers = r.UserAnswers.Where(a => existingAnswers.Any(ea => ea.Id == a.Id)).ToList();
        //     var newAnswers = r.UserAnswers.Where(a => !existingAnswers.Any(ea => ea.Id == a.Id)).ToList();
        //
        //     _dbContext.UserAnswer.UpdateRange(updatedAnswers);
        //     _dbContext.UserAnswer.AddRange(newAnswers);
        //
        //     var answersToRemove = existingAnswers.Where(ea => !r.UserAnswers.Any(a => a.Id == ea.Id)).ToList();
        //     _dbContext.UserAnswer.RemoveRange(answersToRemove);
        // }


        // _dbContext.Entry(userExam).CurrentValues.SetValues(r);
        //
        // Delete children
        if (userExam.UserAnswers != null)
        {
            foreach (var existingChild in userExam.UserAnswers)
                if (r.UserAnswers != null && r.UserAnswers.All(c => c.Id != existingChild.Id))
                    _dbContext.UserAnswer.Remove(existingChild);

            // Update and Insert children
            if (r.UserAnswers != null)
                foreach (var answer in r.UserAnswers)
                {
                    var existingChild = userExam.UserAnswers
                        .FirstOrDefault(x => x.Id == answer.Id);

                    if (existingChild != null)
                        // Update child
                        _dbContext.Entry(existingChild).CurrentValues.SetValues(answer);

                    else
                        _dbContext.UserAnswer.Add(answer);
                }
        }

        await _dbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "data berhasil diupdate");
    }

    public async Task<ServiceResponse> Delete(Guid Id)
    {
        var find = await _dbContext.UserExam.FindAsync(Id);
        if (find == null) return new ServiceResponse(false, "data tidak ditemukan");

        _dbContext.UserExam.Remove(find);
        await _dbContext.SaveChangesAsync();
        return new ServiceResponse(true, "data berhasil dihapus");
    }

    public async Task<UserExam> Get(Guid Id, CancellationToken ct)
    {
        var find = await _dbContext
            .UserExam
            .AsSplitQuery()
            .Where(x => x.Id == Id)
            .Include(x => x.UserAnswers)!
            .ThenInclude(y => y.SoalJawaban)
            .Include(x => x.UserAnswers)!
            .ThenInclude(y => y.Soal)
            .ThenInclude(d => d.PilihanJawaban)
            .FirstOrDefaultAsync(ct);
        if (find == null) return null;

        return find;
    }

    public async Task<UserExam> GetOnly(Guid Id)
    {
        var find = _dbContext
            .UserExam
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == Id);
        if (find == null) return null;

        return find;
    }

    public async Task<IEnumerable<UserExam>> GetAll(Guid RoomId, CancellationToken ct)
    {
        var find = _dbContext
            .UserExam
            .AsNoTracking()
            .Where(x => x.RoomId == RoomId)
            .Include(x => x.UserAnswers)
            .Include(x => x.User)
            .AsSplitQuery();
        return find;
    }

    public async Task<PaginatedResponse<UserExam>> Find(FindRequest r, CancellationToken ct, string? UserId = "")
    {
        if (string.IsNullOrEmpty(UserId))
        {
            var auth = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = auth.User;
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        var data = await _dbContext
            .UserExam
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.UserAnswers)!
            .ThenInclude(y => y.SoalJawaban)
            .Include(x => x.UserAnswers)!
            .ThenInclude(y => y.Soal)
            .Include(x => x.Room)
            .Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.Room.JadwalStart)
            .ToPaginatedList(r.Page, r.PageSize, r.OrderBy, r.Direction, ct);
        return data;
    }

    public async Task<ServiceResponse> UpdateJawaban(UpdateJawabanDTO r, CancellationToken ct)
    {
        var data = await _dbContext.UserAnswer.FirstOrDefaultAsync(x => x.Id == r.UserAnswerId, ct);
        if (data == null)
            return new ServiceResponse(false, "data tidak ditemukan");
        var exam = await _dbContext.UserExam.FirstOrDefaultAsync(x => x.Id == r.UserExamId, ct);
        var entry = _dbContext.Entry(data);
        entry.Entity.SoalJawabanId = r.SoalJawabanId;

        var entryExam = _dbContext.Entry(exam);
        entryExam.Entity!.TimeLeft = r.TimeLeft;
        await _dbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "data berhasil diupdate");
    }

    public async Task<List<UserAnswer>> GetUserAnswers(Guid UserExamId, CancellationToken ct)
    {
        var data = await _dbContext.UserAnswer
            .Where(x => x.UserExamId == UserExamId)
            .ToListAsync(ct);
        return data;
    }

    public async Task<ServiceResponse> RetryExam(Guid UserExamId, CancellationToken ct)
    {
        var data = await _dbContext
            .UserExam
            .Include(x => x.UserAnswers)
            .FirstOrDefaultAsync(x => x.Id == UserExamId, ct);
        if (data == null)
            return new ServiceResponse(false, "data tidak ditemukan");

        var duration = await _dbContext.Room
            .Where(x => x.Id == data.RoomId)
            .Select(x => x.Durasi)
            .FirstOrDefaultAsync(ct);
        foreach (var item in data.UserAnswers) item.SoalJawabanId = null;

        data.IsDone = false;
        data.IsOngoing = false;
        data.RetryCount -= 1;
        data.TimeLeft = TimeSpan.FromMinutes(duration);
        await _dbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "data berhasil diupdate");
    }

    public async Task<ServiceResponse> StartExam(Guid UserExamId, CancellationToken ct)
    {
        var data = await _dbContext
            .UserExam
            .FirstOrDefaultAsync(x => x.Id == UserExamId, ct);
        if (data == null)
            return new ServiceResponse(false, "data tidak ditemukan");
        data.IsOngoing = true;
        data.StartDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return new ServiceResponse(true, "data berhasil diupdate");
    }
}