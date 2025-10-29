namespace IvanovItog.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Type { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
}
