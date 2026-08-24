using CoreLib.Users;

namespace CoreLib.Dashboards;

public class AdminDashboardData
{
    public int TotalPeserta { get; set; }
    public int TotalDosen { get; set; }
    public int TotalOperator { get; set; }
    public int TotalSoal { get; set; }
    public int TotalRuangan { get; set; }
    public int TotalUser { get; set; }
    public int TotalUjian { get; set; }
    public int TotalUjianSelesai { get; set; }
    public int TotalUjianBelumSelesai { get; set; }
    public IEnumerable<InactiveUser> InactiveUsers { get; set; }
    public IEnumerable<LatestLoginUser> LatestLoginUsers { get; set; }
}

public class InactiveUser
{
    public InactiveUser()
    {
    }

    public InactiveUser(ApplicationUser user)
    {
        NamaLengkap = user.NamaLengkap;
        Email = user.Email;
    }

    public string NamaLengkap { get; set; }
    public string Email { get; set; }
}

public class LatestLoginUser
{
    public LatestLoginUser()
    {
    }

    public LatestLoginUser(ApplicationUser user)
    {
        NamaLengkap = user.NamaLengkap;
        LastLogin = (DateTime)user.LastLogin;
    }

    public string NamaLengkap { get; set; }
    public DateTime LastLogin { get; set; }
}