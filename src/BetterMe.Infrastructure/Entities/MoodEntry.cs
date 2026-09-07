using BetterMe.Shared.Enums;

namespace BetterMe.Infrastructure.Entities;

public class MoodEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public MoodLevel Level { get; set; }
    public string? Note { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
