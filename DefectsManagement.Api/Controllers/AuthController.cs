using DefectsManagement.Api.Services;
using DefectsManagement.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DefectsManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private const string AccessTokenCookieName = "DefectsAccessToken";

        public AuthController(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        [HttpGet("get-password-hash/{password}")]
        [AllowAnonymous]
        public IActionResult GetPasswordHash(string password)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(new { passwordHash = hash });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _authService.ValidateUser(model.Username, model.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Неверный логин или пароль." });
            }

            var jwtToken = _authService.GenerateJwtToken(user);

            var useSecureCookies = _config.GetValue<bool>("AppSettings:UseSecureCookies", true);

            // --- режим отладки (возврат токена в теле) ---
            if (!useSecureCookies)
            {
                return Ok(new
                {
                    token = jwtToken,
                    message = "Вход выполнен успешно. Токен возвращен в теле ответа для отладки/Swagger."
                });
            }

            // --- кука для продакшена/Docker ---
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,                    // обязательно для HTTPS
                SameSite = SameSiteMode.None,      // чтобы фронт на frontend.localhost мог отправлять куку
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/",                       // кука будет доступна для всех эндпоинтов
                // Domain = "api.localhost"        // ❌ НЕ указываем, пусть браузер сам выставит домен
            };

            Response.Cookies.Append(AccessTokenCookieName, jwtToken, cookieOptions);

            return Ok(new { message = "Вход выполнен успешно." });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            Response.Cookies.Delete(AccessTokenCookieName, cookieOptions);
            return Ok(new { message = "Выход выполнен успешно." });
        }
    }
}
