using System.ComponentModel.DataAnnotations;
using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Auth;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Patient;
}
