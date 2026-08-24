namespace CoreLib.BankSoal;

public class ExamView
{
    public ExamView()
    {
    }

    public ExamView(Exam exam)
    {
        Id = exam.Id;
        Nama = exam.Nama;
    }

    public int Id { get; set; }
    public string Nama { get; set; }
    public List<SoalView>? Soals { get; set; }

    public int TotalSoal
    {
        get
        {
            if (Soals == null || !Soals.Any())
                return 0;
            return Soals?.Count ?? 0;
        }
    }
}