using CoreLib.RoomSet;

namespace CoreLib.TakeExam;

public class UserExamView
{
    public UserExamView()
    {
    }

    public UserExamView(UserExam userExam)
    {
        Id = userExam.Id;
        IsOngoing = userExam.IsOngoing;
        IsDone = userExam.IsDone;
        RoomId = userExam.RoomId;
        Room = userExam.Room != null ? new RoomView(userExam.Room) : null;
        StartDate = userExam.StartDate;
        EndDate = userExam.EndDate;
        TimeLeft = userExam.TimeLeft;
        RetryCount = userExam.RetryCount;
        ScoreData = userExam.ScoreData;
        ScoreNormalizeData = userExam.ScoreNormalizeData;
        HistoryScoreNormalize = userExam.HistoryScoreNormalize;
    }

    public Guid Id { get; set; }
    public bool IsOngoing { get; set; }
    public bool IsDone { get; set; }
    public Guid RoomId { get; set; }
    public RoomView? Room { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeSpan TimeLeft { get; set; }
    public int? RetryCount { get; set; }
    public int? ScoreData { get; set; }
    public double? ScoreNormalizeData { get; set; }
    public List<double>? HistoryScoreNormalize { get; set; }
}