using System.ComponentModel.DataAnnotations;

namespace DefectsManagement.Api.DTOs;

public class CreateDefectDto
{
    [Required(ErrorMessage = "Заголовок дефекта обязателен.")]
    public required string Title {get; set;}
    [Required(ErrorMessage = "Описание дефекта обязательно.")]
    public required string Description {get; set;}
    [Required(ErrorMessage = "Приоритет дефекта обязателен.")]
    public required string Priority {get; set;}
    public int? AssigneeId {get; set;}
}