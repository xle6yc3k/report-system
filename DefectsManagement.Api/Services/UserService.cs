// Services/UserService.cs
using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using DefectsManagement.Api.DTOs;
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

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        // проверяем уникальность username
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        return newUser;
    }
}
