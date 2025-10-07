namespace DefectsManagement.Api.DTOs;

public class DefectResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public AssignedToDto? AssignedTo { get; set; }
}

public class AssignedToDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Username { get; set; }
}