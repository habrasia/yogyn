using Yogyn.Api.Events;
using Yogyn.Api.Models;

namespace Yogyn.Api.Helpers;

public static class BookingEventFactory
{
    public static BookingCreatedEvent CreateBookingCreatedEvent(
        Booking booking,
        Session session,
        bool isReturningCustomer)
    {
        return new BookingCreatedEvent
        {
            BookingId = booking.Id,
            CreatedAt = DateTime.UtcNow,
            FirstName = booking.FirstName,
            LastName = booking.LastName,
            Email = booking.Email,
            Phone = booking.Phone,
            SessionId = session.Id,
            SessionTitle = session.Title,
            SessionStartsAt = session.StartsAt,
            SessionDuration = session.DurationMinutes,
            StudioName = session.Studio.Name,
            Status = booking.Status,
            CancelToken = booking.CancelToken,
            IsReturningCustomer = isReturningCustomer
        };
    }

    public static BookingApprovedEvent CreateBookingApprovedEvent(Booking booking)
    {
        return new BookingApprovedEvent
        {
            BookingId = booking.Id,
            CreatedAt = DateTime.UtcNow,
            FirstName = booking.FirstName,
            LastName = booking.LastName,
            Email = booking.Email,
            SessionTitle = booking.Session.Title,
            SessionStartsAt = booking.Session.StartsAt,
            SessionDuration = booking.Session.DurationMinutes,
            StudioName = booking.Studio?.Name ?? "",
            CancelToken = booking.CancelToken
        };
    }

    public static BookingRejectedEvent CreateBookingRejectedEvent(
        Booking booking,
        string? reason)
    {
        return new BookingRejectedEvent
        {
            BookingId = booking.Id,
            CreatedAt = DateTime.UtcNow,
            FirstName = booking.FirstName,
            LastName = booking.LastName,
            Email = booking.Email,
            SessionTitle = booking.Session.Title,
            SessionStartsAt = booking.Session.StartsAt,
            StudioName = booking.Studio?.Name ?? "",
            Reason = reason
        };
    }

    public static BookingCancelledEvent CreateBookingCancelledEvent(Booking booking)
    {
        return new BookingCancelledEvent
        {
            BookingId = booking.Id,
            CreatedAt = DateTime.UtcNow,
            FirstName = booking.FirstName,
            LastName = booking.LastName,
            Email = booking.Email,
            SessionTitle = booking.Session.Title,
            SessionStartsAt = booking.Session.StartsAt,
            StudioName = booking.Studio?.Name ?? ""
        };
    }
}
