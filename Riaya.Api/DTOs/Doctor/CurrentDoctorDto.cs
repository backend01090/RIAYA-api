namespace Riaya.Api.DTOs.Doctor;

public class CurrentDoctorDto
{
    public int DoctorId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? SpecializationId { get; set; }
    public string? SpecializationName { get; set; }
    public bool IsActive { get; set; }
}
