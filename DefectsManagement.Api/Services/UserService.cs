// DefectsManagement.Api/Services/UserService.cs (Реализация IUserService)
using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DefectsManagement.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context; 
        
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            // 1. Ищем пользователя по целочисленному ID.
            // Используем SingleOrDefaultAsync, так как FindAsync может быть не всегда оптимален
            // для сущностей, которые могут быть загружены в контекст.
            var user = await _context.Users
                                     .SingleOrDefaultAsync(u => u.Id == userId); 

            if (user == null)
            {
                return (false, new[] { "Пользователь не найден." });
            }

            // 2. Проверяем старый пароль с использованием BCrypt.
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                return (false, new[] { "Неверный текущий пароль." });
            }

            // 3. Хешируем новый пароль.
            // BCrypt сам генерирует случайную соль в процессе хеширования.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            // 4. Сохраняем изменения в базе данных.
            try
            {
                await _context.SaveChangesAsync();
                return (true, Enumerable.Empty<string>());
            }
            catch (DbUpdateException)
            {
                // В случае ошибки сохранения (например, проблема с подключением)
                return (false, new[] { "Ошибка сохранения нового пароля в базе данных." });
            }
        }
    }
}