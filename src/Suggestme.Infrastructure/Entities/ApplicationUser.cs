using Microsoft.AspNetCore.Identity;
using Suggestme.Shared.Enums;

namespace Suggestme.Infrastructure.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsOnboarded { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    public Subscription? Subscription { get; set; }
    public ICollection<Session> PatientSessions { get; set; } = new List<Session>();
    public ICollection<Session> PsychologistSessions { get; set; } = new List<Session>();
    public ICollection<MoodEntry> MoodEntries { get; set; } = new List<MoodEntry>();
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public ICollection<CheckIn> SentCheckIns { get; set; } = new List<CheckIn>();
    public ICollection<CheckIn> ReceivedCheckIns { get; set; } = new List<CheckIn>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public OnboardingResponse? OnboardingResponse { get; set; }
    public ICollection<Resource> CreatedResources { get; set; } = new List<Resource>();
}
