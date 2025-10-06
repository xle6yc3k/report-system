using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class DefectHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid DefectId { get; set; }
    [ForeignKey(nameof(DefectId))] public Defect Defect { get; set; } = null!;

    [Required, MaxLength(50)] public string Type { get; set; } = null!;
    [Required] public Guid ActorId { get; set; }
    [Required] public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [Required] public string PayloadJson { get; set; } = "{}";
}
