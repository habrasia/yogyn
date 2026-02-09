namespace Yogyn.Api.Models;

public class Studio
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Timezone { get; set; }
    public StudioStatus Status { get; set; } = StudioStatus.Active;
    
    public bool RequiresApproval { get; set; } = false;
    public bool AutoApproveReturning { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Session> Sessions { get; set; } = new();
    public List<StudioUser> StudioUsers { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
}

public enum StudioStatus
{
    Active = 0,
    Suspended = 1
}