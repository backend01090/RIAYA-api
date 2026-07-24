namespace Riaya.Api.Entities;

public class Department : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ClinicRoom> ClinicRooms { get; set; } = new List<ClinicRoom>();
}
