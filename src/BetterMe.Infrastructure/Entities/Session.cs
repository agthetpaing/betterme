using BetterMe.Shared.Enums;

namespace BetterMe.Infrastructure.Entities;

public class Session
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PsychologistId { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public SessionStatus Status { get; set; } = SessionStatus.Pending;
    public string? MeetingUrl { get; set; }
    public string? PatientNotes { get; set; }
    public string? PsychologistNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Patient { get; set; } = null!;
    public ApplicationUser Psychologist { get; set; } = null!;
}
