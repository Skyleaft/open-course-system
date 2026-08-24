using System.ComponentModel.DataAnnotations;
using CoreLib.BankSoal;
using CoreLib.Common;

namespace CoreLib.RoomSet;

public class Room : IGenericModifier
{
    public Guid Id { get; set; }

    [Required] [MaxLength(50)] public string Nama { get; set; }

    [Required] [MaxLength(30)] public string Kode { get; set; }

    public DateTime JadwalStart { get; set; }
    public DateTime JadwalEnd { get; set; }
    public int Durasi { get; set; }
    public bool IsActive { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsShowKunci { get; set; }
    public int? RetryCount { get; set; }
    public Exam? Exam { get; set; }
    public int ExamId { get; set; }
    public byte[]? Thumbnail { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}