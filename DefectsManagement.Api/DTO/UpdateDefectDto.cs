namespace DefectsManagement.Api.DTO;

public class UpdateDefectDTO
{
    public string? Title {get; set;}
    public string? Description {get; set;}
    public string? Priority {get; set;}
    public string? Status {get; set;}
    public int? AssigneeId {get; set;}
}