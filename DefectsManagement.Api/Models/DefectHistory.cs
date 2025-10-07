using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

public class DefectHistory
{
    public Guid Id { get; set; }

    public Guid DefectId { get; set; }
    public Defect Defect { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string Payload { get; set; } = "{}";
}
