using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Data;
using Yogyn.Api.Models;

namespace Yogyn.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly YogynDbContext _context;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(YogynDbContext context, ILogger<SessionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/sessions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSessions([FromQuery] Guid? studioId = null)
    {
        _logger.LogInformation("Fetching sessions, studioId filter: {StudioId}", studioId);

        var query = _context.Sessions
            .Where(s => s.Status == SessionStatus.Active);

        if (studioId.HasValue)
        {
            query = query.Where(s => s.StudioId == studioId.Value);
        }

        var sessions = await query
            .Include(s => s.Studio)
            .Select(s => new
            {
                s.Id,
                s.StudioId,
                StudioName = s.Studio.Name,
                StudioSlug = s.Studio.Slug,
                s.Title,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                BookedCount = s.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                SpotsLeft = s.Capacity - s.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                IsFull = s.Bookings.Count(b => b.Status == BookingStatus.Confirmed) >= s.Capacity,
                s.Status,
                s.CreatedAt
            })
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        return Ok(sessions);
    }

    // GET: api/sessions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult> GetSession(Guid id)
    {
        _logger.LogInformation("Fetching session {SessionId}", id);

        var session = await _context.Sessions
            .Include(s => s.Studio)
            .Include(s => s.Bookings.Where(b => b.Status == BookingStatus.Confirmed))
            .Where(s => s.Status == SessionStatus.Active)
            .Select(s => new
            {
                s.Id,
                s.StudioId,
                StudioName = s.Studio.Name,
                StudioSlug = s.Studio.Slug,
                s.Title,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                BookedCount = s.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                SpotsLeft = s.Capacity - s.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                IsFull = s.Bookings.Count(b => b.Status == BookingStatus.Confirmed) >= s.Capacity,
                Participants = s.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .Select(b => new
                    {
                        b.Id,
                        b.FirstName,
                        b.LastName,
                        b.Email,
                        b.Phone,
                        b.AttendanceStatus,
                        b.CreatedAt
                    })
                    .OrderBy(b => b.CreatedAt)
                    .ToList(),
                s.Status,
                s.CreatedAt
            })
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found", id);
            return NotFound(new { error = "Session not found" });
        }

        return Ok(session);
    }

    // POST: api/sessions
    [HttpPost]
    public async Task<ActionResult> CreateSession([FromBody] CreateSessionDto dto)
    {
        _logger.LogInformation("Creating session for studio {StudioId}", dto.StudioId);

        // Validate studio exists and is active
        var studioExists = await _context.Studios.AnyAsync(s => s.Id == dto.StudioId && s.Status == StudioStatus.Active);
        if (!studioExists)
        {
            _logger.LogWarning("Studio {StudioId} not found or inactive", dto.StudioId);
            return BadRequest(new { error = "Studio not found or inactive" });
        }

        // Validate capacity
        if (dto.Capacity <= 0)
        {
            return BadRequest(new { error = "Capacity must be greater than 0" });
        }

        // Validate duration
        if (dto.DurationMinutes <= 0)
        {
            return BadRequest(new { error = "Duration must be greater than 0" });
        }

        // Validate start time is in future
        if (dto.StartsAt <= DateTime.UtcNow)
        {
            return BadRequest(new { error = "Start time must be in the future" });
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            StudioId = dto.StudioId,
            Title = dto.Title,
            StartsAt = dto.StartsAt,
            DurationMinutes = dto.DurationMinutes,
            Capacity = dto.Capacity,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} created successfully", session.Id);

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, new
        {
            session.Id,
            session.StudioId,
            session.Title,
            session.StartsAt,
            session.DurationMinutes,
            session.Capacity,
            session.Status,
            session.CreatedAt
        });
    }

    // PUT: api/sessions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionDto dto)
    {
        _logger.LogInformation("Updating session {SessionId}", id);

        var session = await _context.Sessions.FindAsync(id);
        if (session == null || session.Status == SessionStatus.Cancelled)
        {
            _logger.LogWarning("Session {SessionId} not found or cancelled", id);
            return NotFound(new { error = "Session not found or cancelled" });
        }

        // Validate capacity isn't less than current bookings
        var currentBookings = await _context.Bookings
            .CountAsync(b => b.SessionId == id && b.Status == BookingStatus.Confirmed);

        if (dto.Capacity < currentBookings)
        {
            return BadRequest(new { error = $"Cannot reduce capacity below current bookings ({currentBookings})" });
        }

        session.Title = dto.Title;
        session.StartsAt = dto.StartsAt;
        session.DurationMinutes = dto.DurationMinutes;
        session.Capacity = dto.Capacity;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} updated successfully", id);

        return NoContent();
    }

    // DELETE: api/sessions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelSession(Guid id)
    {
        _logger.LogInformation("Cancelling session {SessionId}", id);

        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found", id);
            return NotFound(new { error = "Session not found" });
        }

        session.Status = SessionStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} cancelled successfully", id);

        return NoContent();
    }
}

// DTOs
public record CreateSessionDto(
    Guid StudioId,
    string Title,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity
);

public record UpdateSessionDto(
    string Title,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity
);