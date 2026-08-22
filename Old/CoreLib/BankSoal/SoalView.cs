namespace CoreLib.BankSoal;

public class SoalView
{
    public Guid Id { get; set; }
    public int ExamId { get; set; }
    public int Nomor { get; set; }
    public string Pertanyaan { get; set; }
    public List<SoalJawabanView>? PilihanJawaban { get; set; }
    public bool isMultipleJawaban { get; set; }
}