using Suggestme.Shared.Enums;

namespace Suggestme.Shared.DTOs.Mood;

public class MoodEntryDto
{
    public Guid Id { get; set; }
    public MoodLevel Level { get; set; }
    public string? Note { get; set; }
    public DateTime RecordedAt { get; set; }
}
