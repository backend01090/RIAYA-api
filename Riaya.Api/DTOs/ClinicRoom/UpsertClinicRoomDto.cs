using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.ClinicRoom;

public class UpsertClinicRoomDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}
