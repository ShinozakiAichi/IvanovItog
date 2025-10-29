namespace IvanovItog.Domain.Entities;

public class Status
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Request> Requests { get; set; } = new List<Request>();
}
