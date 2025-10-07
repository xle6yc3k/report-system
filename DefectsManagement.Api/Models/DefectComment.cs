using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class DefectComment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid DefectId { get; set; }
    [ForeignKey(nameof(DefectId))] public Defect Defect { get; set; } = null!;

    [Required] public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    
    [Required, MaxLength(10_000)] public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
}
