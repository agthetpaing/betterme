namespace Suggestme.Infrastructure.Entities;

public class CheckIn
{
    public Guid Id { get; set; }
    public string PsychologistId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Psychologist { get; set; } = null!;
    public ApplicationUser Patient { get; set; } = null!;
}
