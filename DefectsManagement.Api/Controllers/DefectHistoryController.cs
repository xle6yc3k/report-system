using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/Defect/{defectId:guid}/history")]
[Authorize]
public class DefectHistoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public DefectHistoryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetHistory(Guid defectId, int skip = 0, int take = 100)
    {
        var exists = await _db.Defects.AnyAsync(d => d.Id == defectId);
        if (!exists) return NotFound("Defect not found.");

        var items = await _db.DefectHistories
            .Where(h => h.DefectId == defectId)
            .OrderByDescending(h => h.OccurredAt)
            .Skip(skip).Take(Math.Clamp(take, 1, 500))
            .Select(h => new {
                h.Id, h.Type, h.ActorId, h.OccurredAt, h.Payload
            })
            .ToListAsync();

        return Ok(items);
    }
}
