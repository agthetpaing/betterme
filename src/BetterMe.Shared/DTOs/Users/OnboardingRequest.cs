using System.ComponentModel.DataAnnotations;
using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Users;

public class OnboardingRequest
{
    [Required]
    public List<string> PrimaryGoals { get; set; } = new();

    [Required]
    public List<string> CurrentChallenges { get; set; } = new();

    public List<ResourceType> PreferredResourceTypes { get; set; } = new();

    [Range(1, 5)]
    public int SleepQuality { get; set; }

    [Range(1, 5)]
    public int StressLevel { get; set; }

    public bool HasPreviousTherapy { get; set; }
}
