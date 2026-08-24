namespace CoreLib.RoomSet;

public class RoomScheduller
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
}