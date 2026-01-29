using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Data;
using Yogyn.Api.Models;

namespace Yogyn.Api.Controllers;

[ApiController]
[Route("api/studios")]
public class StudiosController : ControllerBase
{
    private readonly YogynDbContext _context;
    private readonly ILogger<StudiosController> _logger;

    public StudiosController(YogynDbContext context, ILogger<StudiosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active studios with session and user counts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetStudios()
    {
        _logger.LogInformation("Fetching all active studios");
        
        var studios = await _context.Studios
            .Where(s => s.Status == StudioStatus.Active)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Slug,
                s.Timezone,
                s.Status,
                s.CreatedAt,
                SessionCount = s.Sessions.Count(sess => sess.Status == SessionStatus.Active),
                UserCount = s.StudioUsers.Count
            })
            .OrderBy(s => s.Name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active studios", studios.Count);

        return Ok(studios);
    }

    /// <summary>
    /// Get a single studio by ID with active sessions
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetStudio(Guid id)
    {
        _logger.LogInformation("Fetching studio {StudioId}", id);
        
        var studio = await _context.Studios
            .Where(s => s.Status == StudioStatus.Active)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Slug,
                s.Timezone,
                s.Status,
                s.CreatedAt,
                Sessions = s.Sessions
                    .Where(sess => sess.Status == SessionStatus.Active)
                    .OrderBy(sess => sess.StartsAt)
                    .Select(sess => new
                    {
                        sess.Id,
                        sess.Title,
                        sess.StartsAt,
                        sess.DurationMinutes,
                        sess.Capacity,
                        BookedCount = sess.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                        SpotsLeft = sess.Capacity - sess.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                        IsFull = sess.Bookings.Count(b => b.Status == BookingStatus.Confirmed) >= sess.Capacity
                    })
                    .ToList(),
                UserCount = s.StudioUsers.Count
            })
            .FirstOrDefaultAsync(s => s.Id == id);

        if (studio == null)
        {
            _logger.LogWarning("Studio {StudioId} not found or suspended", id);
            return NotFound(new { error = "Studio not found" });
        }

        return Ok(studio);
    }

    /// <summary>
    /// Create a new studio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateStudio([FromBody] CreateStudioDto dto)
    {
        _logger.LogInformation("Creating studio with slug {Slug}", dto.Slug);

        var normalizedSlug = dto.Slug.Trim().ToLower();
        
        if (await _context.Studios.AnyAsync(s => s.Slug.ToLower() == normalizedSlug))
        {
            _logger.LogWarning("Slug {Slug} already exists", normalizedSlug);
            return Conflict(new { error = "A studio with this slug already exists" });
        }

        var studio = new Studio
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Slug = normalizedSlug,
            Timezone = dto.Timezone.Trim(),
            Status = StudioStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Studios.Add(studio);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Studio {StudioId} created successfully with slug {Slug}", studio.Id, studio.Slug);

        return CreatedAtAction(nameof(GetStudio), new { id = studio.Id }, new
        {
            studio.Id,
            studio.Name,
            studio.Slug,
            studio.Timezone,
            studio.Status,
            studio.CreatedAt
        });
    }

    /// <summary>
    /// Update studio details (name and timezone)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudio(Guid id, [FromBody] UpdateStudioDto dto)
    {
        _logger.LogInformation("Updating studio {StudioId}", id);
        
        var studio = await _context.Studios.FindAsync(id);
        if (studio == null || studio.Status == StudioStatus.Suspended)
        {
            _logger.LogWarning("Studio {StudioId} not found or suspended", id);
            return NotFound(new { error = "Studio not found" });
        }

        studio.Name = dto.Name.Trim();
        studio.Timezone = dto.Timezone.Trim();

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Studio {StudioId} updated successfully", id);

        return NoContent();
    }

    /// <summary>
    /// Delete studio (soft delete - sets status to Suspended)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudio(Guid id)
    {
        _logger.LogInformation("Soft-deleting studio {StudioId}", id);
        
        var studio = await _context.Studios.FindAsync(id);
        if (studio == null)
        {
            _logger.LogWarning("Studio {StudioId} not found", id);
            return NotFound(new { error = "Studio not found" });
        }

        studio.Status = StudioStatus.Suspended;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Studio {StudioId} suspended successfully", id);

        return NoContent();
    }
}

// DTOs
public record CreateStudioDto(string Name, string Slug, string Timezone);
public record UpdateStudioDto(string Name, string Timezone);