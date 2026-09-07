using System.ComponentModel.DataAnnotations;
using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Mood;

public class LogMoodRequest
{
    [Required]
    public MoodLevel Level { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
