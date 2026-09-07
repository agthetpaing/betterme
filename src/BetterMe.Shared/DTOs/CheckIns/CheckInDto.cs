namespace BetterMe.Shared.DTOs.CheckIns;

public class CheckInDto
{
    public Guid Id { get; set; }
    public string PsychologistId { get; set; } = string.Empty;
    public string PsychologistName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}
