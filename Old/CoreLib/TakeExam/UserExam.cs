using System.ComponentModel.DataAnnotations;
using CoreLib.RoomSet;
using CoreLib.Users;

namespace CoreLib.TakeExam;

public class UserExam
{
    public Guid Id { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(100)] [Required] public string UserId { get; set; }

    public bool IsOngoing { get; set; }
    public bool IsDone { get; set; }
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeSpan TimeLeft { get; set; }
    public int? RetryCount { get; set; }
    public List<UserAnswer>? UserAnswers { get; set; } = new();

    public int? CalculateScore
    {
        get
        {
            if (UserAnswers != null)
                return UserAnswers.Sum(x => x.SoalJawaban?.Point ?? 0);
            return 0;
        }
    }

    public int? ScoreData { get; set; }

    public double? CalculateScoreNormalize
    {
        get
        {
            if (UserAnswers != null && UserAnswers.Count > 0 && UserAnswers.First().Soal != null &&
                CalculateScore != null && CalculateScore != 0)
                return (double)CalculateScore / UserAnswers.Sum(x => x.Soal?.MaxPoint ?? 0) * 100;
            return 0;
        }
    }

    public double? ScoreNormalizeData { get; set; }
    public List<double>? HistoryScoreNormalize { get; set; }
}