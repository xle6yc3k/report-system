using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class ProjectMember
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid ProjectId { get; set; }
    [Required] public Guid UserId { get; set; }
    [Required] public ProjectRole Role { get; set; }

    [ForeignKey(nameof(ProjectId))] public Project Project { get; set; } = null!;
}
