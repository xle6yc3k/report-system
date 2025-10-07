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
public class ProjectController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProjectController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await _db.Projects
            .Select(p => new { p.Id, p.Key, p.Name, members = p.Members.Count })
            .ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) =>
        Ok(await _db.Projects
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Where(p => p.Id == id)
            .Select(p => new {
                p.Id, p.Key, p.Name,
                members = p.Members.Select(m => new { m.UserId, m.Role, m.User.Name, m.User.Username })
            }).FirstOrDefaultAsync() ?? (object)NotFound());

    [HttpPost, Authorize(Roles="Manager")]
    public async Task<IActionResult> Create(ProjectCreateDto dto)
    {
        if (await _db.Projects.AnyAsync(x => x.Key == dto.Key))
            return Conflict(new { message = "Project key already exists." });
        var p = new Project { Id = Guid.NewGuid(), Key = dto.Key, Name = dto.Name };
        _db.Projects.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id:guid}"), Authorize(Roles="Manager")]
    public async Task<IActionResult> Update(Guid id, ProjectUpdateDto dto)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p is null) return NotFound();
        p.Key = dto.Key; p.Name = dto.Name;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/members"), Authorize(Roles="Manager")]
    public async Task<IActionResult> AddMember(Guid id, AddMemberDto dto)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == dto.UserId)) return NotFound("User not found");
        var roleOk = new[] {"Engineer","Manager","Observer"}.Contains(dto.Role);
        if (!roleOk) return BadRequest("Invalid role");
        var exists = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == id && pm.UserId == dto.UserId);
        if (exists) return Conflict("Already a member");

        _db.ProjectMembers.Add(new ProjectMember {
            ProjectId = id, UserId = dto.UserId, Role = Enum.Parse<ProjectRole>(dto.Role)
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}"), Authorize(Roles="Manager")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var pm = await _db.ProjectMembers.FindAsync(id, userId);
        if (pm is null) return NotFound();
        _db.ProjectMembers.Remove(pm);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
