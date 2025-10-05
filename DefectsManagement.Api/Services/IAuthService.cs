using DefectsManagement.Api.Models; 

namespace DefectsManagement.Api.Services 
{
    public interface IAuthService
    {
        Task<User?> ValidateUser(string username, string password); 
        
        string GenerateJwtToken(User user); 
    }
}