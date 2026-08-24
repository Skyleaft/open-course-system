using System.ComponentModel.DataAnnotations;
using CoreLib.BankSoal;

namespace CoreLib.RoomSet;

public class RoomView
{
    public RoomView()
    {
    }

    public RoomView(Room room)
    {
        Id = room.Id;
        Nama = room.Nama;
        Kode = room.Kode;
        JadwalStart = room.JadwalStart;
        JadwalEnd = room.JadwalEnd;
        Durasi = room.Durasi;
        IsActive = room.IsActive;
        IsRepeatable = room.IsRepeatable;
        RetryCount = room.RetryCount;
        Exam = new ExamView(room.Exam);
        ExamId = room.ExamId;
        Thumbnail = room.Thumbnail;
    }

    public Guid Id { get; set; }

    [Required] [MaxLength(50)] public string Nama { get; set; }

    [Required] [MaxLength(30)] public string Kode { get; set; }

    public DateTime JadwalStart { get; set; }
    public DateTime JadwalEnd { get; set; }
    public int Durasi { get; set; }
    public bool IsActive { get; set; }
    public bool IsRepeatable { get; set; }
    public int? RetryCount { get; set; }
    public ExamView? Exam { get; set; }
    public int ExamId { get; set; }
    public byte[]? Thumbnail { get; set; }
}