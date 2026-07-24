namespace Riaya.Api.Entities;

public class Visit : BaseEntity
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
