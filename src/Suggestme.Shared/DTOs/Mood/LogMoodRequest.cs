using System.ComponentModel.DataAnnotations;
using Suggestme.Shared.Enums;

namespace Suggestme.Shared.DTOs.Mood;

public class LogMoodRequest
{
    [Required]
    public MoodLevel Level { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
