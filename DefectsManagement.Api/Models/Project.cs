using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Models;

[Index(nameof(Key), IsUnique = true)]
public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Короткий код проекта (например: CORE, MOB, WEB)
    [Required, MaxLength(50)]
    public string Key { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public List<ProjectMember> Members { get; set; } = new();
    public List<Defect> Defects { get; set; } = new();
}
