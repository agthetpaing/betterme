using System.ComponentModel.DataAnnotations;

namespace BetterMe.Shared.DTOs.CheckIns;

public class SendCheckInRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}
