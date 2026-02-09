namespace Yogyn.Api.Events;

public class BookingCancelledEvent : IEvent
{
    public Guid BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime SessionStartsAt { get; set; }
    
    public string StudioName { get; set; } = string.Empty;
}
