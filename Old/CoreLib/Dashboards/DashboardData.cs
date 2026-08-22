using CoreLib.TakeExam;

namespace CoreLib.Dashboards;

public class DashboardData
{
    public int TotalUjian { get; set; }
    public double? Avg { get; set; } = 0;
    public IEnumerable<UserExamView> UjianBerlangsung { get; set; }
    public IEnumerable<UserExamView> UjianAkanDatang { get; set; }
    public IEnumerable<UserExamView> RiwayatUjian { get; set; }
}