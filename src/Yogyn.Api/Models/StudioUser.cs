namespace Yogyn.Api.Models;

public class StudioUser
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Studio Studio { get; set; } = null!;
}