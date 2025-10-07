namespace DefectsManagement.Api.Models;

public class User
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }

    // если у тебя ещё используется глобальная роль — оставь:
    public required string Role { get; set; }

    // 👇 навигация для связи ProjectMember → User
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
