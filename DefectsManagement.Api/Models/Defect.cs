using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class Defect
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))] public Project Project { get; set; } = null!;

    [Required, MaxLength(200)] public string Title { get; set; } = null!;
    [Required, MaxLength(10_000)] public string Description { get; set; } = null!;

    [Required] public DefectStatus Status { get; set; } = DefectStatus.New;
    [Required] public DefectPriority Priority { get; set; } = DefectPriority.Medium;

    [Required] public Guid CreatedById { get; set; }
    public Guid? AssignedId { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public bool IsDeleted { get; set; }

    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<DefectTag> Tags { get; set; } = new();
    public List<DefectComment> Comments { get; set; } = new();
    public List<DefectAttachment> Attachments { get; set; } = new();
    public List<DefectHistory> History { get; set; } = new();
}
