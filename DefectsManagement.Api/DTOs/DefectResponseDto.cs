namespace DefectsManagement.Api.DTOs;

public class DefectResponseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public Guid? AssignedId { get; set; }
    public AssignedToDto? AssignedTo { get; set; }
}

public class AssignedToDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Username { get; set; }
}