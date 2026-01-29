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

    /// <summary>
    /// Get all bookings with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBookings(
        [FromQuery] Guid? sessionId = null,
        [FromQuery] string? email = null)
    {
        _logger.LogInformation("Fetching bookings, sessionId: {SessionId}, email: {Email}", sessionId, email);

        var query = _context.Bookings
            .Include(b => b.Session)
            .Include(b => b.Studio)
            .Where(b => b.Status == BookingStatus.Confirmed);

        if (sessionId.HasValue)
        {
            query = query.Where(b => b.SessionId == sessionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(b => b.Email == email.Trim().ToLower());
        }

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
                b.AttendanceStatus,
                b.CreatedAt
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("Found {Count} bookings", bookings.Count);

        return Ok(bookings);
    }

    /// <summary>
    /// Get a single booking by ID
    /// </summary>
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
        {
            _logger.LogWarning("Booking {BookingId} not found", id);
            return NotFound(new { error = "Booking not found" });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Create a new booking (public endpoint)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        _logger.LogInformation("Creating booking for session {SessionId}, email {Email}", dto.SessionId, dto.Email);

        // Validate email format
        if (!IsValidEmail(dto.Email))
        {
            _logger.LogWarning("Invalid email format: {Email}", dto.Email);
            return BadRequest(new { error = "Invalid email format" });
        }

        var normalizedEmail = dto.Email.Trim().ToLower();

        // Get session with current bookings
        var session = await _context.Sessions
            .Include(s => s.Studio)
            .Include(s => s.Bookings.Where(b => b.Status == BookingStatus.Confirmed))
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId && s.Status == SessionStatus.Active);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found or cancelled", dto.SessionId);
            return NotFound(new { error = "Session not found or cancelled" });
        }

        // Check capacity
        var currentBookings = session.Bookings.Count;
        if (currentBookings >= session.Capacity)
        {
            _logger.LogWarning("Session {SessionId} is full - Capacity: {Capacity}, Booked: {Booked}", 
                dto.SessionId, session.Capacity, currentBookings);
            return BadRequest(new
            {
                error = "Session is full",
                capacity = session.Capacity,
                booked = currentBookings,
                message = "This session has reached maximum capacity"
            });
        }

        // Check for duplicate booking
        var existingBooking = await _context.Bookings.AnyAsync(b =>
            b.SessionId == dto.SessionId &&
            b.Email == normalizedEmail &&
            b.Status == BookingStatus.Confirmed);

        if (existingBooking)
        {
            _logger.LogWarning("Duplicate booking attempt - Email {Email} already booked session {SessionId}", 
                normalizedEmail, dto.SessionId);
            return Conflict(new { error = "You have already booked this session" });
        }

        // Create booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StudioId = session.StudioId,
            SessionId = dto.SessionId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            Phone = dto.Phone?.Trim(),
            Status = BookingStatus.Confirmed,
            CancelToken = Guid.NewGuid(),
            AttendanceStatus = AttendanceStatus.NotCheckedIn,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} created successfully for session {SessionId}", 
            booking.Id, dto.SessionId);

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
            booking.CancelToken,
            CancelUrl = $"/api/bookings/cancel/{booking.CancelToken}",
            Message = "Booking confirmed! You will receive a confirmation email shortly.",
            SpotsLeft = session.Capacity - currentBookings - 1
        });
    }

    /// <summary>
    /// Cancel a booking via email link (uses GET for email compatibility)
    /// </summary>
    [HttpGet("cancel/{token}")]
    public async Task<ActionResult> CancelBooking(Guid token)
    {
        _logger.LogInformation("Processing cancellation for token {Token}", token);

        var booking = await _context.Bookings
            .Include(b => b.Session)
            .Include(b => b.Studio)
            .FirstOrDefaultAsync(b => b.CancelToken == token);

        if (booking == null)
        {
            _logger.LogWarning("Invalid cancellation token: {Token}", token);
            return NotFound(new { error = "Invalid cancellation link" });
        }

        // Already cancelled - idempotent
        if (booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogInformation("Booking {BookingId} already cancelled - idempotent response", booking.Id);
            return Ok(new
            {
                message = "This booking has already been cancelled",
                sessionTitle = booking.Session.Title,
                sessionStartsAt = booking.Session.StartsAt,
                studioName = booking.Studio.Name,
                alreadyCancelled = true
            });
        }

        // Check if session already started
        if (booking.Session.StartsAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Cannot cancel booking {BookingId} - session already started", booking.Id);
            return BadRequest(new
            {
                error = "Cannot cancel - session has already started",
                sessionTitle = booking.Session.Title,
                sessionStartsAt = booking.Session.StartsAt,
                studioName = booking.Studio.Name
            });
        }

        // Cancel booking
        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} cancelled successfully", booking.Id);

        return Ok(new
        {
            message = "Booking cancelled successfully",
            sessionTitle = booking.Session.Title,
            sessionStartsAt = booking.Session.StartsAt,
            studioName = booking.Studio.Name,
            cancelled = true
        });
    }

    /// <summary>
    /// Update attendance status
    /// </summary>
    [HttpPatch("{id}/attendance")]
    public async Task<IActionResult> UpdateAttendance(Guid id, [FromBody] UpdateAttendanceDto dto)
    {
        _logger.LogInformation("Updating attendance for booking {BookingId} to {Status}", id, dto.AttendanceStatus);

        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null || booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogWarning("Booking {BookingId} not found or cancelled", id);
            return NotFound(new { error = "Booking not found or cancelled" });
        }

        booking.AttendanceStatus = dto.AttendanceStatus;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attendance updated successfully for booking {BookingId}", id);

        return NoContent();
    }

    // Email validation helper
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
}

// DTOs
public record CreateBookingDto(
    Guid SessionId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone
);

public record UpdateAttendanceDto(
    AttendanceStatus AttendanceStatus
);