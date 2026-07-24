namespace Riaya.Api.Entities;

public class ClinicRoom : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DoctorClinicAssignment> DoctorAssignments { get; set; } = new List<DoctorClinicAssignment>();
}
