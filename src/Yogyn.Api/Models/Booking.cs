namespace Yogyn.Api.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Guid SessionId { get; set; }
    
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public Guid CancelToken { get; set; } = Guid.NewGuid();
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotCheckedIn;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Studio Studio { get; set; } = null!;
    public Session Session { get; set; } = null!;
}

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1
}

public enum AttendanceStatus
{
    NotCheckedIn = 0,
    Present = 1,
    NoShow = 2
}