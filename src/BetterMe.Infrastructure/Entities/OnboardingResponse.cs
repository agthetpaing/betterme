namespace BetterMe.Infrastructure.Entities;

public class OnboardingResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PrimaryGoals { get; set; } = "[]";
    public string CurrentChallenges { get; set; } = "[]";
    public string PreferredResourceTypes { get; set; } = "[]";
    public int SleepQuality { get; set; }
    public int StressLevel { get; set; }
    public bool HasPreviousTherapy { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
