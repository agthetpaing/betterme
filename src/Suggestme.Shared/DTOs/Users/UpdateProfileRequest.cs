using System.ComponentModel.DataAnnotations;

namespace Suggestme.Shared.DTOs.Users;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }

    public string? ProfileImageUrl { get; set; }
}
