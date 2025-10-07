// Services/UserService.cs
using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid userId)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public Task<User?> GetByUsernameAsync(string username)
        => _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return false;

        // проверяем текущий пароль
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return false;

        // записываем новый хеш
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }
}
