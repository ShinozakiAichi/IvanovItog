using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string Login { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Request> CreatedRequests { get; set; } = new List<Request>();
    public ICollection<Request> AssignedRequests { get; set; } = new List<Request>();
}
