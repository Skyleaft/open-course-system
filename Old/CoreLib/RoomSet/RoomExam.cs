using CoreLib.TakeExam;

namespace CoreLib.RoomSet;

public class RoomExam
{
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
    public IEnumerable<UserExam>? UserExams { get; set; }
    public int? TotalPeserta => UserExams?.Count() ?? 0;
}