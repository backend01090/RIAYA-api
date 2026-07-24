namespace Riaya.Api.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
}
