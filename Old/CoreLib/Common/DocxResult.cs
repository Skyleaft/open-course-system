using CoreLib.BankSoal;

namespace CoreLib.Common;
public class DocxResult
{
    public string FileName { get; set; } = string.Empty;
    public int TotalSoal { get; set; }
    public List<Soal> Soals { get; set; } = new();
    public List<KunciJawaban> KunJaw { get; set; } = new();
    public List<string> UnprocesKunci { get; set; } = new();

}