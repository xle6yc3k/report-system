using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DefectsManagement.Api.Controllers;

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

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException("Missing NameIdentifier claim");
        return Guid.Parse(idStr);
    }

    // GET /api/User/me
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role     = User.FindFirstValue(ClaimTypes.Role);

        if (username is null || role is null)
            return Unauthorized(new { message = "Токен не содержит необходимых клеймов." });

        var userId = GetCurrentUserId(User);

        var dto = new UserInfoDto
        {
            Id       = userId,
            Username = username,
            Role     = role
        };

        return Ok(dto);
    }

    // PUT /api/User/me/password
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId(User);

        var ok = await _userService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);
        if (!ok)
            return BadRequest(new { message = "Неверный текущий пароль или пользователь не найден." });

        return NoContent();
    }
}
