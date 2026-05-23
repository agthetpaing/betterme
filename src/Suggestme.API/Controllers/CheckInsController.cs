using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suggestme.Infrastructure.Data;
using Suggestme.Infrastructure.Entities;
using Suggestme.Shared.DTOs.CheckIns;
using System.Security.Claims;

namespace Suggestme.API.Controllers;

[ApiController]
[Route("api/checkins")]
[Authorize]
public class CheckInsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CheckInsController(AppDbContext db) => _db = db;

    /// <summary>Patient retrieves check-ins sent to them.</summary>
    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyCheckIns()
    {
        var userId = GetUserId();

        var checkIns = await _db.CheckIns
            .Include(c => c.Psychologist)
            .Where(c => c.PatientId == userId)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync();

        return Ok(checkIns.Select(MapToDto));
    }

    /// <summary>Psychologist sends a check-in message to a patient.</summary>
    [HttpPost]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> SendCheckIn([FromBody] SendCheckInRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var psychId = GetUserId();

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            PsychologistId = psychId,
            PatientId = request.PatientId,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };

        _db.CheckIns.Add(checkIn);
        await _db.SaveChangesAsync();

        await _db.Entry(checkIn).Reference(c => c.Psychologist).LoadAsync();

        return CreatedAtAction(nameof(GetMyCheckIns), MapToDto(checkIn));
    }

    /// <summary>Mark a check-in as read.</summary>
    [HttpPut("{id:guid}/read")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        var checkIn = await _db.CheckIns.FirstOrDefaultAsync(c => c.Id == id && c.PatientId == userId);
        if (checkIn == null) return NotFound();

        checkIn.IsRead = true;
        checkIn.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    private static CheckInDto MapToDto(CheckIn c) => new()
    {
        Id = c.Id,
        PsychologistId = c.PsychologistId,
        PsychologistName = $"{c.Psychologist?.FirstName} {c.Psychologist?.LastName}".Trim(),
        PatientId = c.PatientId,
        Message = c.Message,
        IsRead = c.IsRead,
        SentAt = c.SentAt
    };
}
