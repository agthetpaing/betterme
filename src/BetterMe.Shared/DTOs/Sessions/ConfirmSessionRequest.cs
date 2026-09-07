using System.ComponentModel.DataAnnotations;

namespace BetterMe.Shared.DTOs.Sessions;

public class ConfirmSessionRequest
{
    [Required]
    public string MeetingUrl { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? PsychologistNotes { get; set; }
}
