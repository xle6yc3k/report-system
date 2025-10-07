using System.ComponentModel.DataAnnotations;

namespace DefectsManagement.Api.DTOs;

public class CreateDefectDto
{
    public Guid ProjectId { get; init; }
    [Required(ErrorMessage = "Заголовок дефекта обязателен.")]
    public required string Title {get; set;}
    [Required(ErrorMessage = "Описание дефекта обязательно.")]
    public required string Description {get; set;}
    [Required(ErrorMessage = "Приоритет дефекта обязателен.")]
    public string? Priority {get; set;}
    public Guid? AssignedId { get; init; }
    public DateOnly? DueDate { get; init; }
    public List<string>? Tags { get; init; }
}