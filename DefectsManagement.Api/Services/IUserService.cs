using DefectsManagement.Api.Models;
using DefectsManagement.Api.DTOs;

namespace DefectsManagement.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);

    Task<User> CreateUserAsync(CreateUserDto dto);
}
