using DefectsManagement.Api.Models;

namespace DefectsManagement.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
}
