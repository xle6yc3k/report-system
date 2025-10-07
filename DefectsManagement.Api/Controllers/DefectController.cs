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

    // === helpers ===

    private Guid GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new UnauthorizedAccessException("No NameIdentifier");
        return Guid.Parse(id);
    }

    private static string ToString(DefectPriority p) => p.ToString();
    private static string ToString(DefectStatus s) => s.ToString();

    private static bool TryParsePriority(string? value, out DefectPriority result)
        => Enum.TryParse(value, ignoreCase: true, out result);

    private static bool TryParseStatus(string? value, out DefectStatus result)
        => Enum.TryParse(value, ignoreCase: true, out result);

    private static DefectResponseDto ToResponseDto(Defect d)
        => new()
        {
            Id = d.Id,
            Title = d.Title,
            Description = d.Description,
            Priority = ToString(d.Priority),   // DTO ожидает string
            Status = ToString(d.Status),       // DTO ожидает string
            CreatedAt = d.CreatedAt,
            AssignedTo = d.AssignedTo != null
                ? new AssignedToDto
                {
                    Id = d.AssignedTo.Id,
                    Name = d.AssignedTo.Name,
                    Username = d.AssignedTo.Username
                }
                : null
        };

    private bool IsManager()  => User.IsInRole("Manager");
    private bool IsEngineer() => User.IsInRole("Engineer");
    // Observer есть, но он только на чтение — отдельная проверка не нужна

    // === 1) Create ===
    [HttpPost]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> CreateDefect([FromBody] CreateDefectDto dto)
    {
        // базовые проверки
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");
        if (dto.ProjectId == Guid.Empty)
            return BadRequest("ProjectId is required.");

        // если передан assignedId — разрешаем только менеджеру
        if (dto.AssignedId.HasValue && !IsManager())
            return Forbid("Only Manager can set AssignedId on create.");

        // проверить, что проект существует (минимально)
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
        if (!projectExists) return BadRequest("Project not found.");

        // проверить существование исполнителя (если указан)
        if (dto.AssignedId.HasValue)
        {
            var assigneeExists = await _context.Users.AnyAsync(u => u.Id == dto.AssignedId.Value);
            if (!assigneeExists) return BadRequest("Assigned user not found.");
        }

        // приоритет: в DTO он строкой → парсим; по умолчанию Medium
        var priority = DefectPriority.Medium;
        if (!string.IsNullOrWhiteSpace(dto.Priority))
        {
            if (!TryParsePriority(dto.Priority, out priority))
                return BadRequest("Invalid priority.");
        }

        var actorId = GetCurrentUserId();

        var defect = new Defect
        {
            ProjectId   = dto.ProjectId,
            Title       = dto.Title,
            Description = dto.Description,
            Priority    = priority,
            Status      = DefectStatus.New,
            CreatedById = actorId,
            AssignedId  = IsManager() ? dto.AssignedId : null,
            DueDate     = IsManager() ? dto.DueDate : null,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        // теги, если пришли
        if (dto.Tags is { Count: > 0 })
            defect.Tags = dto.Tags.Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Select(t => new DefectTag { DefectId = defect.Id, Tag = t })
                                 .ToList();

        _context.Defects.Add(defect);
        await _context.SaveChangesAsync();

        // для ответа подтянем исполнителя (если есть)
        await _context.Entry(defect).Reference(d => d.AssignedTo).LoadAsync();

        return CreatedAtAction(nameof(GetDefect), new { id = defect.Id }, ToResponseDto(defect));
    }

    // === 2) List ===
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DefectResponseDto>>> GetDefects()
    {
        var items = await _context.Defects
            .Include(d => d.AssignedTo)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(ToResponseDto));
    }

    // === 3) Get by id ===
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DefectResponseDto>> GetDefect(Guid id)
    {
        var defect = await _context.Defects
            .Include(d => d.AssignedTo)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (defect == null) return NotFound("Defect not found.");

        return Ok(ToResponseDto(defect));
    }

    // === 4) Update (PUT) ===
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> UpdateDefect(Guid id, [FromBody] UpdateDefectDto dto)
    {
        var defect = await _context.Defects
            .Include(d => d.AssignedTo)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (defect == null) return NotFound("Defect not found.");

        var actorId  = GetCurrentUserId();
        var isMgr    = IsManager();
        var isEng    = IsEngineer();

        // Ограничение для Engineer: можно править только свои/назначенные и только неп финальные
        if (isEng)
        {
            var isMine = defect.CreatedById == actorId || defect.AssignedId == actorId;
            if (!isMine) return Forbid("Engineer can update only own/assigned defects.");

            if (defect.Status is DefectStatus.Closed or DefectStatus.Canceled)
                return Forbid("Engineer cannot edit final defects.");
        }

        // Текстовые поля — всем, кто прошёл проверку выше
        if (!string.IsNullOrWhiteSpace(dto.Title)) defect.Title = dto.Title!;
        if (!string.IsNullOrWhiteSpace(dto.Description)) defect.Description = dto.Description!;

        // Priority (string → enum): только менеджер
        if (dto.Priority is not null)
        {
            if (!isMgr) return Forbid("Only Manager can change priority.");
            if (!TryParsePriority(dto.Priority, out var prio)) return BadRequest("Invalid priority.");
            defect.Priority = prio;
        }

        // Status (string → enum): инженер — только в рамках собственных дефектов; менеджер — любой
        if (dto.Status is not null)
        {
            if (!TryParseStatus(dto.Status, out var st)) return BadRequest("Invalid status.");
            // (валидацию переходов по графу добавишь через IWorkflowService; тут просто назначаем)
            if (isEng)
            {
                // при необходимости, проверь допустимость перехода здесь
            }
            defect.Status = st;
        }

        // AssignedId: только менеджер. null — снимаем исполнителя.
        if (dto.AssignedIdSet) // см. пояснение ниже
        {
            if (!isMgr) return Forbid("Only Manager can (re)assign.");
            if (dto.AssignedId is null)
            {
                defect.AssignedId = null; // снять
            }
            else
            {
                var exists = await _context.Users.AnyAsync(u => u.Id == dto.AssignedId.Value);
                if (!exists) return BadRequest("Assigned user not found.");
                defect.AssignedId = dto.AssignedId;
            }
        }

        // DueDate: только менеджер
        if (dto.DueDateSet)
        {
            if (!isMgr) return Forbid("Only Manager can change due date.");
            defect.DueDate = dto.DueDate; // может быть null
        }

        defect.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
