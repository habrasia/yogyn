using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Data;
using Yogyn.Api.Models;

namespace Yogyn.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly YogynDbContext _context;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(YogynDbContext context, ILogger<BookingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/bookings?sessionId=X&email=Y&status=Z
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBookings(
        [FromQuery] Guid? sessionId = null,
        [FromQuery] string? email = null,
        [FromQuery] BookingStatus? status = null)
    {
        _logger.LogInformation("Fetching bookings");

        var query = _context.Bookings
            .Include(b => b.Session)
            .Include(b => b.Studio)
            .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected);

        if (sessionId.HasValue)
            query = query.Where(b => b.SessionId == sessionId.Value);

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(b => b.Email == email.Trim().ToLower());

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var bookings = await query
            .Select(b => new
            {
                b.Id,
                b.SessionId,
                SessionTitle = b.Session.Title,
                SessionStartsAt = b.Session.StartsAt,
                SessionDuration = b.Session.DurationMinutes,
                b.StudioId,
                StudioName = b.Studio.Name,
                b.FirstName,
                b.LastName,
                b.Email,
                b.Phone,
                b.Status,
                b.AttendanceStatus,
                b.CreatedAt
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings);
    }

    // GET: api/bookings/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult> GetBooking(Guid id)
    {
        _logger.LogInformation("Fetching booking {BookingId}", id);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .Include(b => b.Studio)
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.SessionId,
                SessionTitle = b.Session.Title,
                SessionStartsAt = b.Session.StartsAt,
                SessionDuration = b.Session.DurationMinutes,
                b.StudioId,
                StudioName = b.Studio.Name,
                b.FirstName,
                b.LastName,
                b.Email,
                b.Phone,
                b.Status,
                b.AttendanceStatus,
                b.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        return Ok(booking);
    }

    // POST: api/bookings
    [HttpPost]
    public async Task<ActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        _logger.LogInformation("Creating booking for session {SessionId}", dto.SessionId);

        if (!IsValidEmail(dto.Email))
            return BadRequest(new { error = "Invalid email format" });

        var normalizedEmail = dto.Email.Trim().ToLower();

        // Get session with studio and bookings (confirmed + pending)
        var session = await _context.Sessions
            .Include(s => s.Studio)
            .Include(s => s.Bookings.Where(b => 
                b.Status == BookingStatus.Confirmed || 
                b.Status == BookingStatus.Pending))
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId && s.Status == SessionStatus.Active);

        if (session == null)
            return NotFound(new { error = "Session not found or cancelled" });

        // Check capacity (confirmed + pending count against capacity)
        var currentBookings = session.Bookings.Count;
        if (currentBookings >= session.Capacity)
        {
            return BadRequest(new
            {
                error = "Session is full",
                capacity = session.Capacity,
                booked = currentBookings
            });
        }

        // Check duplicate (confirmed OR pending)
        var existingBooking = await _context.Bookings.AnyAsync(b =>
            b.SessionId == dto.SessionId &&
            b.Email == normalizedEmail &&
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending));

        if (existingBooking)
            return Conflict(new { error = "You have already booked this session" });

        // Check if returning customer (has attended at least once)
        var isReturningCustomer = await _context.Bookings.AnyAsync(b =>
            b.Email == normalizedEmail &&
            b.StudioId == session.StudioId &&
            b.Status == BookingStatus.Confirmed &&
            b.AttendanceStatus == AttendanceStatus.Present);

        // Determine initial status
        var initialStatus = DetermineBookingStatus(
            session.Studio.RequiresApproval,
            session.Studio.AutoApproveReturning,
            isReturningCustomer
        );

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StudioId = session.StudioId,
            SessionId = dto.SessionId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            Phone = dto.Phone?.Trim(),
            Status = initialStatus,
            CancelToken = Guid.NewGuid(),
            AttendanceStatus = AttendanceStatus.NotCheckedIn,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} created with status {Status}", booking.Id, initialStatus);

        var message = GetBookingMessage(initialStatus, isReturningCustomer);

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, new
        {
            booking.Id,
            booking.SessionId,
            SessionTitle = session.Title,
            SessionStartsAt = session.StartsAt,
            SessionDuration = session.DurationMinutes,
            StudioName = session.Studio.Name,
            booking.FirstName,
            booking.LastName,
            booking.Email,
            booking.Phone,
            booking.Status,
            booking.CancelToken,
            CancelUrl = $"/api/bookings/cancel/{booking.CancelToken}",
            Message = message,
            RequiresApproval = session.Studio.RequiresApproval,
            IsReturningCustomer = isReturningCustomer,
            SpotsLeft = session.Capacity - currentBookings - 1
        });
    }

    // POST: api/bookings/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveBooking(Guid id)
    {
        _logger.LogInformation("Approving booking {BookingId}", id);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        if (booking.Status == BookingStatus.Confirmed)
            return Ok(new { message = "Booking already approved", alreadyApproved = true });

        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { error = "Only pending bookings can be approved" });

        // Check capacity (confirmed only, not pending)
        var confirmedCount = await _context.Bookings
            .CountAsync(b => 
                b.SessionId == booking.SessionId && 
                b.Status == BookingStatus.Confirmed);

        if (confirmedCount >= booking.Session.Capacity)
        {
            return BadRequest(new
            {
                error = "Session is now full",
                capacity = booking.Session.Capacity,
                confirmed = confirmedCount
            });
        }

        booking.Status = BookingStatus.Confirmed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} approved", booking.Id);

        return Ok(new
        {
            message = "Booking approved successfully",
            bookingId = booking.Id,
            sessionTitle = booking.Session.Title,
            customerEmail = booking.Email,
            approved = true
        });
    }

    // POST: api/bookings/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectBooking(Guid id, [FromBody] RejectReasonDto dto)
    {
        _logger.LogInformation("Rejecting booking {BookingId}", id);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        if (booking.Status == BookingStatus.Rejected)
            return Ok(new { message = "Booking already rejected", alreadyRejected = true });

        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { error = "Only pending bookings can be rejected" });

        booking.Status = BookingStatus.Rejected;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} rejected", booking.Id);

        return Ok(new
        {
            message = "Booking rejected successfully",
            bookingId = booking.Id,
            sessionTitle = booking.Session.Title,
            customerEmail = booking.Email,
            reason = dto.Reason,
            rejected = true
        });
    }

    // GET: api/bookings/cancel/{token}
    [HttpGet("cancel/{token}")]
    public async Task<ActionResult> CancelBookingByToken(Guid token)
    {
        _logger.LogInformation("Processing cancellation token {Token}", token);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.CancelToken == token);

        if (booking == null)
            return NotFound(new { error = "Invalid cancellation link" });

        if (booking.Status == BookingStatus.Cancelled)
        {
            return Ok(new
            {
                message = "This booking has already been cancelled",
                sessionTitle = booking.Session.Title,
                alreadyCancelled = true
            });
        }

        if (booking.Status == BookingStatus.Pending)
        {
            return BadRequest(new
            {
                error = "Cannot cancel pending booking via link. Contact studio directly.",
                sessionTitle = booking.Session.Title
            });
        }

        if (booking.Session.StartsAt <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "Cannot cancel - session has already started",
                sessionTitle = booking.Session.Title
            });
        }

        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} cancelled", booking.Id);

        return Ok(new
        {
            message = "Booking cancelled successfully",
            sessionTitle = booking.Session.Title,
            cancelled = true
        });
    }

    // DELETE: api/bookings/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelBooking(Guid id, [FromBody] CancelReasonDto? dto = null)
    {
        _logger.LogInformation("Admin cancelling booking {BookingId}", id);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        if (booking.Status == BookingStatus.Cancelled)
            return Ok(new { message = "Booking already cancelled", alreadyCancelled = true });

        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} cancelled by admin", booking.Id);

        return Ok(new
        {
            message = "Booking cancelled successfully",
            bookingId = booking.Id,
            sessionTitle = booking.Session.Title,
            customerEmail = booking.Email,
            reason = dto?.Reason,
            cancelled = true
        });
    }

    // PATCH: api/bookings/{id}/attendance
    [HttpPatch("{id}/attendance")]
    public async Task<IActionResult> UpdateAttendance(Guid id, [FromBody] UpdateAttendanceDto dto)
    {
        _logger.LogInformation("Updating attendance for {BookingId}", id);

        var booking = await _context.Bookings.FindAsync(id);
        
        if (booking == null || booking.Status == BookingStatus.Cancelled)
            return NotFound(new { error = "Booking not found or cancelled" });

        booking.AttendanceStatus = dto.AttendanceStatus;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Helpers
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    private BookingStatus DetermineBookingStatus(
        bool requiresApproval,
        bool autoApproveReturning,
        bool isReturningCustomer)
    {
        if (!requiresApproval)
            return BookingStatus.Confirmed;
        
        if (autoApproveReturning && isReturningCustomer)
            return BookingStatus.Confirmed;
        
        return BookingStatus.Pending;
    }

    private string GetBookingMessage(BookingStatus status, bool isReturning)
    {
        return status switch
        {
            BookingStatus.Confirmed when isReturning => "Welcome back! Booking confirmed.",
            BookingStatus.Confirmed => "Booking confirmed! Check your email.",
            BookingStatus.Pending => "Booking received! Waiting for studio approval.",
            _ => "Booking received."
        };
    }
}

// DTOs
public record CreateBookingDto(
    Guid SessionId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone
);

public record UpdateAttendanceDto(AttendanceStatus AttendanceStatus);
public record RejectReasonDto(string? Reason);
public record CancelReasonDto(string? Reason);