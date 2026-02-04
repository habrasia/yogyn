namespace Yogyn.Api.Events;

using Yogyn.Api.Models;

public class BookingCreatedEvent : IEvent
{
    public Guid BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    
    public Guid SessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime SessionStartsAt { get; set; }
    public int SessionDuration { get; set; }
    
    public string StudioName { get; set; } = string.Empty;
    
    public BookingStatus Status { get; set; }
    public Guid CancelToken { get; set; }
    public bool IsReturningCustomer { get; set; }
}
