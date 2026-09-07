using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Users;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public UserRole Role { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsOnboarded { get; set; }
    public DateTime CreatedAt { get; set; }
    public SubscriptionTier? SubscriptionTier { get; set; }
}
