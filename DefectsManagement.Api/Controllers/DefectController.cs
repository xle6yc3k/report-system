using System.Security.Claims;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;
using DefectsManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DefectController : ControllerBase
{
    private readonly IDefectService _svc;

    public DefectController(IDefectService svc)
    {
        _svc = svc;
    }

    // ===== helpers =====

    private Guid GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new UnauthorizedAccessException("No NameIdentifier");
        return Guid.Parse(id);
    }

    private bool IsManager()  => User.IsInRole("Manager");
    private bool IsEngineer() => User.IsInRole("Engineer");

    // ===== DTOs for PATCH (в одном файле для скорости) =====
    public sealed class AssignDto
    {
        public Guid? AssignedId { get; set; }
    }

    public sealed class StatusDto
    {
        public string Status { get; set; } = null!;
    }

    public sealed class PriorityDto
    {
        public string Priority { get; set; } = null!;
    }

    public sealed class DueDateDto
    {
        public DateOnly? DueDate { get; set; }
    }

    public sealed class TagsPutDto
    {
        public List<string> Tags { get; set; } = new();
    }

    // ===== 1) Create =====
    [HttpPost]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> CreateDefect([FromBody] CreateDefectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");
        if (dto.ProjectId == Guid.Empty)
            return BadRequest("ProjectId is required.");

        // Engineer не может задавать assignee/dueDate — сервис сам проигнорирует
        var actor = GetCurrentUserId();
        var created = await _svc.CreateAsync(dto, actor, IsManager(), IsEngineer());
        return CreatedAtAction(nameof(GetDefect), new { id = created.Id }, created);
    }

    // ===== 2) List (простой список; фильтры можно будет докинуть позже) =====
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DefectResponseDto>>> GetDefects()
    {
        var items = await _svc.ListAsync();
        return Ok(items);
    }

    // ===== 3) Get by id =====
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DefectResponseDto>> GetDefect(Guid id)
    {
        var d = await _svc.GetAsync(id);
        if (d is null) return NotFound("Defect not found.");
        return Ok(d);
    }

    // ===== 4) Update (PUT) =====
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> UpdateDefect(Guid id, [FromBody] UpdateDefectDto dto)
    {
        var actor = GetCurrentUserId();
        await _svc.UpdateAsync(id, dto, actor, IsManager(), IsEngineer());
        return NoContent();
    }

    // ===== 5) PATCH: assign (Manager) =====
    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignDto body)
    {
        await _svc.AssignAsync(id, body.AssignedId, GetCurrentUserId());
        return NoContent();
    }

    // ===== 6) PATCH: status =====
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] StatusDto body)
    {
        if (!Enum.TryParse(body.Status, true, out DefectStatus newStatus))
            return BadRequest("Invalid status.");

        await _svc.ChangeStatusAsync(id, newStatus, GetCurrentUserId(), IsManager(), IsEngineer());
        return NoContent();
    }

    // ===== 7) PATCH: priority (Manager) =====
    [HttpPatch("{id:guid}/priority")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ChangePriority(Guid id, [FromBody] PriorityDto body)
    {
        if (!Enum.TryParse(body.Priority, true, out DefectPriority prio))
            return BadRequest("Invalid priority.");
        await _svc.ChangePriorityAsync(id, prio, GetCurrentUserId());
        return NoContent();
    }

    // ===== 8) PATCH: due-date (Manager) =====
    [HttpPatch("{id:guid}/due-date")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ChangeDueDate(Guid id, [FromBody] DueDateDto body)
    {
        await _svc.ChangeDueDateAsync(id, body.DueDate, GetCurrentUserId());
        return NoContent();
    }

    // ===== 9) PUT: tags (полная замена) =====
    [HttpPut("{id:guid}/tags")]
    [Authorize(Roles = "Engineer,Manager")]
    public async Task<IActionResult> PutTags(Guid id, [FromBody] TagsPutDto body)
    {
        // В сервисе: Engineer — только свои/назначенные; Manager — любые
        await _svc.PutTagsAsync(id, body.Tags ?? new(), GetCurrentUserId());
        return NoContent();
    }

    // ===== 10) DELETE (soft-delete) =====
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _svc.DeleteAsync(id, GetCurrentUserId());
        return NoContent();
    }
}
