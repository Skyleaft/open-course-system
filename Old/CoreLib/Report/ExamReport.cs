using System.ComponentModel.DataAnnotations.Schema;
using CoreLib.Common;

namespace CoreLib.Report;

public class ExamReport
{
    public Guid Id { get; set; }
    public string NamaLengkap { get; set; }
    public Gender Gender { get; set; }
    public string? AsalKota { get; set; }
    public string? NomorTelepon { get; set; }
    public string NamaRoom { get; set; }
    public DateTime Jadwal { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }

    [Column("nilai")] public double Nilai { get; set; }

    public string UserID { get; set; }
}