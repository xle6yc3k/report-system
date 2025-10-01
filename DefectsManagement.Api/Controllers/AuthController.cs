using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net; // Мы будем использовать эту библиотеку для хэширования паролей

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Вспомогательный метод для генерации JWT токена
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30), // Токен будет действовать 30 минут
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    // Модель для данных входа (логин и пароль)
    public class LoginModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    [HttpPost("login")]
    [AllowAnonymous] // Разрешаем доступ без аутентификации
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == model.Username);

        // Проверка логина и пароля
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            return Unauthorized("Неверный логин или пароль");
        }

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }
    
    [HttpPost("create-user")]
    [Authorize(Roles = "Observer")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserModel model)
    {
            var existingUser = _context.Users.Any(u => u.Username == model.Username);
        if (existingUser)
        {
            return BadRequest($"Пользователь с логином '{model.Username}' уже существует.");
        }

        var allowedRoles = new[] { "Engineer", "Manager", "Observer" };
        if (!allowedRoles.Contains(model.Role))
        {
            return BadRequest($"Недопустимая роль. Разрешены: {string.Join(", ", allowedRoles)}");
        }

        var newUser = new User
        {
            Name = model.Name,
            Username = model.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = model.Role
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        return Ok(new 
        { 
            Message = "Пользователь успешно создан.", 
            Id = newUser.Id, 
            newUser.Name, 
            newUser.Username, 
            newUser.Role 
        });
    }
}