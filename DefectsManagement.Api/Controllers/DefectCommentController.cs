using System.Security.Claims;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/Defect/{defectId:guid}/comments")]
[Authorize]
public class DefectCommentController : ControllerBase
{
    private readonly ICommentService _svc;
    public DefectCommentController(ICommentService svc) => _svc = svc;

    private Guid Me() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsManager() => User.IsInRole("Manager");

    [HttpPost]
    public async Task<IActionResult> Add(Guid defectId, [FromBody] CreateCommentDto dto)
    {
        var id = await _svc.AddAsync(defectId, Me(), dto.Text);
        return CreatedAtAction(nameof(List), new { defectId }, new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid defectId, DateTime? after = null, int limit = 50)
    {
        var items = await _svc.ListAsync(defectId, after, limit);
        return Ok(items);
    }

    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Update(Guid defectId, Guid commentId, [FromBody] UpdateCommentDto dto)
    {
        await _svc.UpdateAsync(defectId, commentId, Me(), dto.Text, IsManager());
        return NoContent();
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid defectId, Guid commentId)
    {
        await _svc.DeleteAsync(defectId, commentId, Me(), IsManager());
        return NoContent();
    }
}
