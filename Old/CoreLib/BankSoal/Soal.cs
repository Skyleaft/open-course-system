namespace CoreLib.BankSoal;

public class Soal
{
    public Guid Id { get; set; }
    public int ExamId { get; set; }
    public int Nomor { get; set; }
    public string Pertanyaan { get; set; }
    public List<SoalJawaban>? PilihanJawaban { get; set; }
    public bool isMultipleJawaban { get; set; }

    public int BobotPoint
    {
        get
        {
            if (PilihanJawaban == null || !PilihanJawaban.Any()) return 0;
            return PilihanJawaban?.Sum(x => x.Point) ?? 0;
        }
    }

    public int MaxPoint
    {
        get
        {
            if (PilihanJawaban == null || !PilihanJawaban.Any()) return 0;
            return PilihanJawaban?.Max(x => x.Point) ?? 0;
        }
    }

    public int MinPoint
    {
        get
        {
            if (PilihanJawaban == null || !PilihanJawaban.Any()) return 0;
            return PilihanJawaban?.Min(x => x.Point) ?? 0;
        }
    }
}