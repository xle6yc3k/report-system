using System.Security.Claims;
using DefectsManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace DefectsManagement.Api.Controllers;

[ApiController]
[Route("api/Defect/{defectId:guid}/attachments")]
[Authorize]
public class DefectAttachmentController : ControllerBase
{
    private readonly IAttachmentService _svc;
    private readonly IConfiguration _cfg;

    public DefectAttachmentController(IAttachmentService svc, IConfiguration cfg)
    {
        _svc = svc;
        _cfg = cfg;
        AppContext.SetData("StorageRoot", _cfg["Storage:Root"] ?? "/storage");
    }

    private Guid Me() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsManager() => User.IsInRole("Manager");
    private string Root() => _cfg["Storage:Root"] ?? "/storage";

    [HttpGet]
    public async Task<IActionResult> List(Guid defectId)
        => Ok(await _svc.ListAsync(defectId));

    // <-- ВАЖНО: принимаем IFormFileCollection, не List<IFormFile>
    [HttpPost]
    [Authorize(Roles = "Manager,Engineer")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid defectId, [FromForm] IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files provided.");

        var ids = await _svc.UploadAsync(defectId, Me(), files, Root());
        return Ok(new { uploaded = ids });
    }

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Download(Guid defectId, Guid fileId)
    {
        var (stream, contentType, downloadName) = await _svc.OpenAsync(defectId, fileId, Me(), IsManager());
        return File(stream, contentType, downloadName);
    }

    [HttpDelete("{fileId:guid}")]
    [Authorize(Roles = "Manager,Engineer")]
    public async Task<IActionResult> Delete(Guid defectId, Guid fileId)
    {
        await _svc.DeleteAsync(defectId, fileId, Me(), IsManager(), Root());
        return NoContent();
    }
}
