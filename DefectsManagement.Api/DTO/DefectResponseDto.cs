namespace DefectsManagement.Api.DTO;

namespace DefectsManagement.Api.DTOs;

public class DefectResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public AssigneeDto? Assignee { get; set; }
}

public class AssigneeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
}