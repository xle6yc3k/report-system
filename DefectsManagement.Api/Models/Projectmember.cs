using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class ProjectMember
{
    [Required] public Guid ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))] public Project Project { get; set; } = null!;

    [Required] public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

    [Required] public ProjectRole Role { get; set; }
}
