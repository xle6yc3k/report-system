using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class DefectAttachment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid DefectId { get; set; }
    [ForeignKey(nameof(DefectId))] public Defect Defect { get; set; } = null!;

    [Required] public Guid UploadedById { get; set; }
    [Required, MaxLength(255)] public string FileName { get; set; } = null!;
    [Required, MaxLength(128)] public string ContentType { get; set; } = null!;
    [Required] public long Size { get; set; }

    // путь/хеш в сторадже
    [Required, MaxLength(512)] public string StorageKey { get; set; } = null!;

    public User UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
