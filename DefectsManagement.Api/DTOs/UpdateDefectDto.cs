namespace DefectsManagement.Api.DTOs;

public class UpdateDefectDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }

    public string? Priority { get; init; }
    public string? Status   { get; init; }

    public Guid? AssignedId { get; init; }
    public bool AssignedIdSet { get; init; }
    
    public DateOnly? DueDate { get; init; }
    public bool DueDateSet { get; init; } 
}
