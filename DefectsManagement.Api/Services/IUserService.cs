namespace DefectsManagement.Api.Services 
{
    public interface IUserService
    {
        Task<(bool IsSuccess, IEnumerable<string> Errors)> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }
}