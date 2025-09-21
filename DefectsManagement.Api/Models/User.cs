namespace DefectsManagement.Api.Models;

// Модель для хранения информации о пользователях
public class User
{
    // Уникальный идентификатор пользователя
    public int Id { get; set; }
    
    // Имя пользователя (например, для отображения)
    public string Name { get; set; }

    // Логин для входа
    public string Username { get; set; }

    // Хэш пароля. Важно: храним не сам пароль, а его зашифрованный вариант
    public string PasswordHash { get; set; }

    // Роль пользователя: Engineer, Manager или Observer
    public string Role { get; set; }
}