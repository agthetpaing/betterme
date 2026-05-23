using System.ComponentModel.DataAnnotations;

namespace Suggestme.Shared.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
