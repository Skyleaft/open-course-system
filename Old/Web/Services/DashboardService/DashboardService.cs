using System.Security.Claims;
using CoreLib.Dashboards;
using CoreLib.TakeExam;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Web.Common.Database;

namespace Web.Services.DashboardService;

public class DashboardService : IDashboard
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly AppDbContext _dbContext;

    public DashboardService(AuthenticationStateProvider authenticationStateProvider, AppDbContext dbContext)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _dbContext = dbContext;
    }


    public async Task<DashboardData> Get(CancellationToken ct, string? UserId = "")
    {
        if (string.IsNullOrEmpty(UserId))
        {
            var auth = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = auth.User;
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        var data = _dbContext
            .UserExam
            .Include(x => x.Room)
            .ThenInclude(y => y.Exam)
            .AsNoTracking()
            .Where(x => x.UserId == UserId);

        var res = new DashboardData
        {
            TotalUjian = await data.CountAsync(ct),
            Avg = await data.AverageAsync(x => x.ScoreNormalizeData, ct) ?? 0,
            RiwayatUjian = data.Select(x => new UserExamView(x)),
            UjianBerlangsung = data.Where(x =>
                    x.Room.JadwalEnd.ToLocalTime() > DateTime.Now.ToLocalTime() &&
                    x.Room.JadwalStart.ToLocalTime() < DateTime.Now.ToLocalTime())
                .Select(x => new UserExamView(x)),
            UjianAkanDatang = data.Where(x => x.Room.JadwalStart.ToLocalTime() > DateTime.Now.ToLocalTime())
                .Select(x => new UserExamView(x))
        };

        return res;
    }

    public async Task<DosenDashboardData> GetDosen(CancellationToken ct, string? UserId = "")
    {
        if (string.IsNullOrEmpty(UserId))
        {
            var auth = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = auth.User;
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        var currentUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == UserId, ct);
        var peserta = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleId == "c6477de3-27b8-4094-b504-48ee4954995e")
            .CountAsync(ct);
        var totalSoal = await _dbContext.Exam
            .AsNoTracking()
            .CountAsync(ct);
        var totalRuang = await _dbContext.Room
            .AsNoTracking()
            .Where(x => x.CreatedBy == currentUser.UserName)
            .CountAsync(ct);

        var data = _dbContext
            .UserExam
            .Include(x => x.User)
            .Include(x => x.Room)
            .ThenInclude(y => y.Exam)
            .AsNoTracking()
            .Where(x => x.IsDone == true);

        var filter = data
            .GroupBy(x => x.User)
            .Select(x => new
            {
                User = x.Key,
                AverageScore = data.Where(y => y.User == x.Key).Average(y => y.ScoreNormalizeData),
                HighestScore = data.Where(y => y.User == x.Key).Max(y => y.ScoreNormalizeData),
                LowestScore = data.Where(y => y.User == x.Key).Min(y => y.ScoreNormalizeData)
            });

        var filterRoom = data
            .GroupBy(x => x.Room)
            .Select(x => new
            {
                Room = x.Key,
                AverageScore = data.Where(y => y.Room == x.Key).Average(y => y.ScoreNormalizeData),
                HighestScore = data.Where(y => y.Room == x.Key).Max(y => y.ScoreNormalizeData),
                LowestScore = data.Where(y => y.Room == x.Key).Min(y => y.ScoreNormalizeData)
            })
            .Where(x => x.Room.CreatedBy == currentUser.UserName);

        var res = new DosenDashboardData
        {
            TotalPeserta = peserta,
            TotalSoal = totalSoal,
            TotalRuangan = totalRuang,
            Top10 = filter.Select(x => new NilaiUser
            {
                Avg = (double)x.AverageScore,
                Highest = (double)x.HighestScore,
                Lowest = (double)x.LowestScore,
                NamaPeserta = x.User.NamaLengkap
            }).OrderByDescending(u => u.Avg).Take(10),
            Bottom10 = filter.Select(x => new NilaiUser
            {
                Avg = (double)x.AverageScore,
                Highest = (double)x.HighestScore,
                Lowest = (double)x.LowestScore,
                NamaPeserta = x.User.NamaLengkap
            }).OrderBy(u => u.Avg).Take(10),
            RoomAnalysis = filterRoom.Select(x => new RoomAnalys
            {
                Avg = x.AverageScore,
                Highest = x.HighestScore,
                Lowest = x.LowestScore,
                NamaRoom = x.Room.Nama,
                Jadwal = x.Room.JadwalStart
            }).OrderByDescending(x => x.Jadwal)
        };

        return res;
    }

    public async Task<AdminDashboardData> GetAdmin(CancellationToken ct)
    {
        var roles = _dbContext.UserRoles.AsNoTracking();
        var peserta = await roles
            .Where(x => x.RoleId == "c6477de3-27b8-4094-b504-48ee4954995e")
            .CountAsync(ct);
        var dosen = await roles
            .Where(x => x.RoleId == "f37731fe-e205-446e-a744-4c90f1f12821")
            .CountAsync(ct);
        var ope = await roles
            .Where(x => x.RoleId == "f37731fe-e205-446e-a744-4c90f1f12821")
            .CountAsync(ct);
        var user = await roles
            .CountAsync(ct);
        var totalSoal = await _dbContext.Exam
            .AsNoTracking()
            .CountAsync(ct);
        var totalRuang = await _dbContext.Room
            .AsNoTracking()
            .CountAsync(ct);
        var totalUjian = await _dbContext.UserExam
            .AsNoTracking()
            .CountAsync(ct);
        var totalUjianSelesai = await _dbContext.UserExam
            .AsNoTracking()
            .Where(x => x.IsDone == true)
            .CountAsync(ct);
        var totalUjianBelumSelesai = totalUjian - totalUjianSelesai;

        var inactiveUser = _dbContext.Users
            .Where(x => x.EmailConfirmed == false).Select(x => new InactiveUser(x));

        var lastLogin = _dbContext.Users
            .Where(x => x.LastLogin != null)
            .OrderByDescending(x => x.LastLogin)
            .Take(10).Select(x => new LatestLoginUser(x));


        var res = new AdminDashboardData
        {
            TotalPeserta = peserta,
            TotalDosen = dosen,
            TotalOperator = ope,
            TotalUser = user,
            TotalSoal = totalSoal,
            TotalRuangan = totalRuang,
            TotalUjian = totalUjian,
            TotalUjianSelesai = totalUjianSelesai,
            TotalUjianBelumSelesai = totalUjianBelumSelesai,
            InactiveUsers = inactiveUser,
            LatestLoginUsers = lastLogin
        };

        return res;
    }
}