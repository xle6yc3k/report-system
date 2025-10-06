using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class DefectTag
{
    [Required] public Guid DefectId { get; set; }
    [ForeignKey(nameof(DefectId))] public Defect Defect { get; set; } = null!;

    [Required, MaxLength(50)] public string Tag { get; set; } = null!;
}
