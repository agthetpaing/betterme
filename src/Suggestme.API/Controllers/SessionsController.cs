using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suggestme.Infrastructure.Data;
using Suggestme.Infrastructure.Entities;
using Suggestme.Shared.DTOs.Sessions;
using Suggestme.Shared.Enums;
using System.Security.Claims;

namespace Suggestme.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Returns sessions for the current user.
    /// Patients see their own sessions; psychologists see sessions assigned to them.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] SessionStatus? status = null)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var query = _db.Sessions
            .Include(s => s.Patient)
            .Include(s => s.Psychologist)
            .AsQueryable();

        query = role == "Psychologist" || role == "Admin"
            ? query.Where(s => s.PsychologistId == userId)
            : query.Where(s => s.PatientId == userId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var sessions = await query
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

        return Ok(sessions.Select(MapToDto));
    }

    /// <summary>Patient books a session with a psychologist.</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> BookSession([FromBody] BookSessionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var patientId = GetUserId();

        // Verify psychologist exists
        var psych = await _db.Users.FindAsync(request.PsychologistId);
        if (psych == null || psych.Role != UserRole.Psychologist)
            return BadRequest("Psychologist not found.");

        var session = new Session
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            PsychologistId = request.PsychologistId,
            ScheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc),
            DurationMinutes = request.DurationMinutes,
            PatientNotes = request.PatientNotes,
            Status = SessionStatus.Pending
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Reload with nav props
        await _db.Entry(session).Reference(s => s.Patient).LoadAsync();
        await _db.Entry(session).Reference(s => s.Psychologist).LoadAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, MapToDto(session));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var session = await _db.Sessions
            .Include(s => s.Patient)
            .Include(s => s.Psychologist)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        var userId = GetUserId();
        if (session.PatientId != userId && session.PsychologistId != userId)
            return Forbid();

        return Ok(MapToDto(session));
    }

    /// <summary>Psychologist confirms the session and provides a meeting URL.</summary>
    [HttpPut("{id:guid}/confirm")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmSessionRequest request)
    {
        var session = await GetOwnedSession(id);
        if (session == null) return NotFound();

        if (session.Status != SessionStatus.Pending)
            return BadRequest("Only pending sessions can be confirmed.");

        session.Status = SessionStatus.Confirmed;
        session.MeetingUrl = request.MeetingUrl;
        if (!string.IsNullOrWhiteSpace(request.PsychologistNotes))
            session.PsychologistNotes = request.PsychologistNotes;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Psychologist declines the session request.</summary>
    [HttpPut("{id:guid}/decline")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> Decline(Guid id)
    {
        var session = await GetOwnedSession(id);
        if (session == null) return NotFound();

        if (session.Status != SessionStatus.Pending)
            return BadRequest("Only pending sessions can be declined.");

        session.Status = SessionStatus.Declined;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Psychologist marks the session as completed.</summary>
    [HttpPut("{id:guid}/complete")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteSessionRequest request)
    {
        var session = await GetOwnedSession(id);
        if (session == null) return NotFound();

        if (session.Status != SessionStatus.Confirmed)
            return BadRequest("Only confirmed sessions can be marked complete.");

        session.Status = SessionStatus.Completed;
        if (!string.IsNullOrWhiteSpace(request.PsychologistNotes))
            session.PsychologistNotes = request.PsychologistNotes;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Cancel a session (patient or psychologist can cancel).</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetUserId();
        var session = await _db.Sessions
            .Include(s => s.Patient)
            .Include(s => s.Psychologist)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();
        if (session.PatientId != userId && session.PsychologistId != userId) return Forbid();

        if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Canceled)
            return BadRequest("Session cannot be cancelled in its current state.");

        session.Status = SessionStatus.Canceled;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Session?> GetOwnedSession(Guid id)
    {
        var userId = GetUserId();
        return await _db.Sessions
            .Include(s => s.Patient)
            .Include(s => s.Psychologist)
            .FirstOrDefaultAsync(s => s.Id == id && s.PsychologistId == userId);
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    private string GetUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? "Patient";

    private static SessionDto MapToDto(Session s) => new()
    {
        Id = s.Id,
        PatientId = s.PatientId,
        PatientName = $"{s.Patient?.FirstName} {s.Patient?.LastName}".Trim(),
        PsychologistId = s.PsychologistId,
        PsychologistName = $"{s.Psychologist?.FirstName} {s.Psychologist?.LastName}".Trim(),
        ScheduledAt = s.ScheduledAt,
        DurationMinutes = s.DurationMinutes,
        Status = s.Status,
        MeetingUrl = s.MeetingUrl,
        PatientNotes = s.PatientNotes,
        PsychologistNotes = s.PsychologistNotes,
        CreatedAt = s.CreatedAt
    };
}

// Inline DTO — only used in this controller
public class CompleteSessionRequest
{
    public string? PsychologistNotes { get; set; }
}
