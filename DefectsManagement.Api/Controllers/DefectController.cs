using DefectsManagement.Api.Data;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DefectController : ControllerBase
{
    private readonly AppDbContext _context;

    public DefectController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        throw new InvalidOperationException("Не удалось получить ID пользователя из токена.");
    }
private static DefectResponseDto ToResponseDto(Defect defect)
    {
        return new DefectResponseDto
        {
            Id = defect.Id,
            Title = defect.Title,
            Description = defect.Description,
            Priority = defect.Priority,
            Status = defect.Status,
            CreatedAt = defect.CreatedAt,
            Assignee = defect.Assignee != null ? new AssigneeDto
            {
                Id = defect.Assignee.Id,
                Name = defect.Assignee.Name,
                Username = defect.Assignee.Username
            } : null
        };
    }

    // --- 1. Создание дефекта (Create) ---
    [HttpPost]
    // Разрешаем создание только Инженерам и Менеджерам
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> CreateDefect([FromBody] CreateDefectDto dto)
    {
        // Проверка существования назначенного исполнителя, если он указан
        if (dto.AssigneeId.HasValue)
        {
            var assigneeExists = await _context.Users.AnyAsync(u => u.Id == dto.AssigneeId.Value);
            if (!assigneeExists)
            {
                return BadRequest("Указанный исполнитель не найден.");
            }
        }
        
        var newDefect = new Defect
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            AssigneeId = dto.AssigneeId,
            // Status по умолчанию будет "Новая", как в модели
            // CreatedAt будет установлено автоматически
        };

        _context.Defects.Add(newDefect);
        await _context.SaveChangesAsync();

        // Загружаем исполнителя для ответа
        await _context.Entry(newDefect).Reference(d => d.Assignee).LoadAsync();

        return CreatedAtAction(nameof(GetDefect), new { id = newDefect.Id }, ToResponseDto(newDefect));
    }

    // --- 2. Просмотр списка дефектов (Read All) ---
    [HttpGet]
    // Доступно всем аутентифицированным пользователям
    public async Task<ActionResult<IEnumerable<DefectResponseDto>>> GetDefects()
    {
        // Запрос дефектов с информацией об исполнителе
        var defects = await _context.Defects
            .Include(d => d.Assignee)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        
        // Преобразуем список моделей в список DTO
        return Ok(defects.Select(ToResponseDto));
    }

    // --- 3. Детальный просмотр дефекта (Read One) ---
    [HttpGet("{id}")]
    // Доступно всем аутентифицированным пользователям
    public async Task<ActionResult<DefectResponseDto>> GetDefect(int id)
    {
        var defect = await _context.Defects
            .Include(d => d.Assignee)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (defect == null)
        {
            return NotFound("Дефект не найден.");
        }

        return Ok(ToResponseDto(defect));
    }

    // --- 4. Обновление дефекта (Update) ---
    [HttpPut("{id}")]
    // Разрешаем обновление только Инженерам и Менеджерам
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> UpdateDefect(int id, [FromBody] UpdateDefectDto dto)
    {
        var defect = await _context.Defects.FindAsync(id);

        if (defect == null)
        {
            return NotFound("Дефект не найден.");
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // --- ЛОГИКА ОГРАНИЧЕНИЯ ДОСТУПА ---
        if (userRole == "Engineer")
        {
            int currentUserId = GetCurrentUserId(); // Получаем ID текущего инженера

            // Инженер может обновлять дефект, только если он назначен исполнителем.
            if (defect.AssigneeId != currentUserId)
            {
                return Forbid("Инженер может обновлять только дефекты, назначенные ему.");
            }
        }
        // Менеджеры имеют полный доступ, поэтому дополнительная проверка не требуется.
        // ------------------------------------
        
        // Обновление полей, если они предоставлены в DTO
        if (dto.Title != null) defect.Title = dto.Title;
        if (dto.Description != null) defect.Description = dto.Description;
        if (dto.Priority != null) defect.Priority = dto.Priority;
        
        // В ТЗ указан жизненный цикл статусов: Новая → В работе → На проверке → Закрыта/Отменена.
        // Здесь требуется более сложная логика, но для CRUD пока просто обновляем статус.
        if (dto.Status != null) defect.Status = dto.Status;
        
        // Обработка назначенного исполнителя (AssigneeId)
        if (dto.AssigneeId.HasValue)
        {
            // Проверка, что новый исполнитель существует
            var assigneeExists = await _context.Users.AnyAsync(u => u.Id == dto.AssigneeId.Value);
            if (!assigneeExists)
            {
                return BadRequest("Указанный исполнитель не найден.");
            }
            defect.AssigneeId = dto.AssigneeId.Value;
        }
        // Специальная обработка, если клиент явно передал "assigneeId": null, чтобы сбросить исполнителя.
        else if (dto.AssigneeId == null && Request.HttpContext.Request.ContentLength > 0)
        {
            // Проверяем тело запроса на наличие "assigneeId": null
            if (Request.Body.CanSeek) Request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            
            // Поиск точного ключа с null-значением
            if (body.Contains("\"assigneeId\":null", StringComparison.OrdinalIgnoreCase) || 
                body.Contains("\"assigneeid\":null", StringComparison.OrdinalIgnoreCase))
            {
                defect.AssigneeId = null;
            }
            
            // Сбрасываем позицию потока для дальнейшей обработки
            Request.Body.Seek(0, SeekOrigin.Begin);
        }
        
        await _context.SaveChangesAsync();

        return NoContent(); // 204 No Content - успешное обновление
    }
}


