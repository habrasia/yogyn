namespace Yogyn.Api.Models;

public class Session
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public required string Title { get; set; }
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Studio Studio { get; set; } = null!;
    public List<Booking> Bookings { get; set; } = new();
}

public enum SessionStatus
{
    Active = 0,
    Cancelled = 1
}