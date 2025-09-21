using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DefectsManagement.Api.Models;

// Модель для хранения информации о дефектах
public class Defect
{
    // Уникальный идентификатор дефекта
    public int Id { get; set; }

    // Заголовок дефекта
    public string Title { get; set; }

    // Полное описание дефекта
    public string Description { get; set; }

    // Приоритет дефекта: Low, Medium, High
    public string Priority { get; set; }

    // Статус дефекта (например, "Новая", "В работе")
    public string Status { get; set; } = "Новая";

    // Идентификатор пользователя, ответственного за исправление дефекта
    public int? AssigneeId { get; set; }

    // Ссылка на объект пользователя (навигационное свойство Entity Framework)
    [ForeignKey("AssigneeId")]
    public User? Assignee { get; set; }

    // Дата создания дефекта
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}