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

    // GET: api/studios
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
            .ToListAsync();

        return Ok(studios);
    }

    // GET: api/studios/{id}
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
                        SpotsLeft = sess.Capacity - sess.Bookings.Count(b => b.Status == BookingStatus.Confirmed)
                    })
                    .ToList(),
                UserCount = s.StudioUsers.Count
            })
            .FirstOrDefaultAsync(s => s.Id == id);

        if (studio == null)
        {
            _logger.LogWarning("Studio {StudioId} not found", id);
            return NotFound(new { error = "Studio not found" });
        }

        return Ok(studio);
    }

    // POST: api/studios
    [HttpPost]
    public async Task<ActionResult> CreateStudio([FromBody] CreateStudioDto dto)
    {
        _logger.LogInformation("Creating studio with slug {Slug}", dto.Slug);
        
        // Validate slug uniqueness
        if (await _context.Studios.AnyAsync(s => s.Slug == dto.Slug))
        {
            _logger.LogWarning("Slug {Slug} already exists", dto.Slug);
            return Conflict(new { error = "A studio with this slug already exists" });
        }

        var studio = new Studio
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug,
            Timezone = dto.Timezone,
            Status = StudioStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Studios.Add(studio);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Studio {StudioId} created successfully", studio.Id);

        return CreatedAtAction(nameof(GetStudio), new { id = studio.Id }, studio);
    }

    // PUT: api/studios/{id}
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

        studio.Name = dto.Name;
        studio.Timezone = dto.Timezone;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Studio {StudioId} updated successfully", id);

        return NoContent();
    }

    // DELETE: api/studios/{id}
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