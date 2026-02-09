using Yogyn.Api.Events;

namespace Yogyn.Api.Services;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(BookingCreatedEvent booking);
    Task SendBookingPendingAsync(BookingCreatedEvent booking);
    Task SendBookingApprovedAsync(BookingApprovedEvent booking);
    Task SendBookingRejectedAsync(BookingRejectedEvent booking);
    Task SendBookingCancelledAsync(BookingCancelledEvent booking);
}
