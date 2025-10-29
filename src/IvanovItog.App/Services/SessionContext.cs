using IvanovItog.Domain.Entities;

namespace IvanovItog.App.Services;

public class SessionContext
{
    public User? CurrentUser { get; set; }
}
