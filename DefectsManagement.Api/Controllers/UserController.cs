using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DefectsManagement.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/User/me
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            // 1. Извлекаем все необходимые клеймы как строки
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdString) || username == null || role == null)
            {
                return Unauthorized(new { message = "Токен не содержит необходимых клеймов." });
            }

            // 2. Преобразуем ID в int (поскольку ID в БД — int)
            if (!int.TryParse(userIdString, out var userIdInt))
            {
                // Ошибка в строке 38 (38,31): The name 'userIdString' does not exist in the current context
                // Исправлено: переменная userIdString теперь корректно объявлена.
                return StatusCode(500, new { message = "Внутренняя ошибка: ID пользователя имеет неверный формат." });
            }

            var userInfo = new UserInfoDto
            {
                Id = userIdInt, // Должен быть int, как в вашем DTO
                Username = username,
                Role = role
            };

            return Ok(userInfo);
        }
        
        // PUT /api/User/me/password
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            // Извлекаем ID как строку
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            // Преобразуем в int для передачи в сервис
            if (!int.TryParse(userIdString, out var userIdInt))
            {
                 return Unauthorized(new { message = "Не удалось идентифицировать пользователя." });
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Ошибки CS0103 и CS8130 в строке 72
            // Исправлено: используем корректную переменную userIdInt
            var (isSuccess, errors) = await _userService.ChangePasswordAsync(userIdInt, model.OldPassword, model.NewPassword);

            if (isSuccess)
            {
                return Ok(new { message = "Пароль успешно изменен." });
            }
            else
            {
                return BadRequest(new { message = "Ошибка смены пароля.", errors = errors });
            }
        }
    }
}