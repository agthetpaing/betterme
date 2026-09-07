using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Sessions;

public class SessionDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PsychologistId { get; set; } = string.Empty;
    public string PsychologistName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public SessionStatus Status { get; set; }
    public string? MeetingUrl { get; set; }
    public string? PatientNotes { get; set; }
    public string? PsychologistNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
