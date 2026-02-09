namespace Yogyn.Api.Events;

public interface IEvent
{
    Guid BookingId { get; }
    DateTime CreatedAt { get; }
}
