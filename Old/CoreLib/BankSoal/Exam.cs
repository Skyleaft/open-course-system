using CoreLib.Common;

namespace CoreLib.BankSoal;

public class Exam : IGenericModifier
{
    public int Id { get; set; }
    public string Nama { get; set; }
    public List<Soal>? Soals { get; set; }

    public int TotalSoal
    {
        get
        {
            if (Soals == null || !Soals.Any())
                return 0;
            return Soals?.Count ?? 0;
        }
    }

    public int TotalPoint
    {
        get
        {
            if (Soals == null || !Soals.Any())
                return 0;
            return Soals?.Sum(x => x.MaxPoint) ?? 0;
        }
    }

    public byte[]? Thumbnail { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}