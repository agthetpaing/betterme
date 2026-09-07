using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.DTOs.Mood;
using System.Security.Claims;

namespace BetterMe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodController : ControllerBase
{
    private readonly AppDbContext _db;

    public MoodController(AppDbContext db) => _db = db;

    /// <summary>Get mood history for the current patient (last 60 days).</summary>
    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyMood([FromQuery] int days = 60)
    {
        var userId = GetUserId();
        var since = DateTime.UtcNow.AddDays(-days);

        var entries = await _db.MoodEntries
            .Where(m => m.UserId == userId && m.RecordedAt >= since)
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToDto));
    }

    /// <summary>Log a mood entry for the current patient.</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> LogMood([FromBody] LogMoodRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var entry = new MoodEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Level = request.Level,
            Note = request.Note,
            RecordedAt = DateTime.UtcNow  // already UTC — fine
        };

        _db.MoodEntries.Add(entry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyMood), MapToDto(entry));
    }

    /// <summary>Psychologist views mood history for a specific patient.</summary>
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> GetPatientMood(string patientId, [FromQuery] int days = 60)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var entries = await _db.MoodEntries
            .Where(m => m.UserId == patientId && m.RecordedAt >= since)
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToDto));
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    private static MoodEntryDto MapToDto(MoodEntry m) => new()
    {
        Id = m.Id,
        Level = m.Level,
        Note = m.Note,
        RecordedAt = m.RecordedAt
    };
}
