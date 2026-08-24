namespace CoreLib.Dashboards;

public class DosenDashboardData
{
    public int TotalPeserta { get; set; }
    public int TotalSoal { get; set; }
    public int TotalRuangan { get; set; }
    public IEnumerable<NilaiUser> Top10 { get; set; }
    public IEnumerable<NilaiUser> Bottom10 { get; set; }
    public IEnumerable<RoomAnalys> RoomAnalysis { get; set; }
}

public class NilaiUser
{
    public string NamaPeserta { get; set; }
    public double? Avg { get; set; }
    public double? Highest { get; set; }
    public double? Lowest { get; set; }
}

public class RoomAnalys
{
    public string NamaRoom { get; set; }
    public DateTime Jadwal { get; set; }
    public double? Avg { get; set; }
    public double? Highest { get; set; }
    public double? Lowest { get; set; }
}