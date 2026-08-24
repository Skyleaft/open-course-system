namespace CoreLib.BankSoal;

public class SoalJawaban
{
    public Guid Id { get; set; }
    public Guid SoalId { get; set; }
    public string Jawaban { get; set; }
    public bool IsBenar { get; set; }
    public int Point { get; set; } = 0;
}