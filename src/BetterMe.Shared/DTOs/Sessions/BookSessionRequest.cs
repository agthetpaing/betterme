using System.ComponentModel.DataAnnotations;

namespace BetterMe.Shared.DTOs.Sessions;

public class BookSessionRequest
{
    [Required]
    public string PsychologistId { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 60;

    [MaxLength(1000)]
    public string? PatientNotes { get; set; }
}
