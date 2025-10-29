using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Entities;

public class Request
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int CategoryId { get; set; }
    public Priority Priority { get; set; }
    public int StatusId { get; set; }
    public int CreatedById { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Category? Category { get; set; }
    public Status? Status { get; set; }
    public User? CreatedBy { get; set; }
    public User? AssignedTo { get; set; }
}
