using System.Text.RegularExpressions;
using DefectsManagement.Api.Data;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Services;

public class AttachmentService : IAttachmentService
{
    private readonly AppDbContext _db;
    public AttachmentService(AppDbContext db) => _db = db;

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".png",".jpg",".jpeg",".pdf",".log",".txt" };

    public async Task<IEnumerable<AttachmentResponseDto>> ListAsync(Guid defectId)
    {
        var list = await _db.DefectAttachments
            .Where(a => a.DefectId == defectId)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new AttachmentResponseDto {
                Id = a.Id, FileName = a.FileName, ContentType = a.ContentType, Size = a.Size,
                UploadedAt = a.UploadedAt, UploadedById = a.UploadedById,
                DownloadUrl = $"/api/Defect/{defectId}/attachments/{a.Id}"
            })
            .ToListAsync();

        return list;
    }

    public async Task<Guid[]> UploadAsync(Guid defectId, Guid actorId, IFormFileCollection files, string storageRoot)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == defectId)
                ?? throw new KeyNotFoundException("Defect not found");

        var isMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == d.ProjectId && pm.UserId == actorId);
        if (!isMember) throw new UnauthorizedAccessException();

        var saved = new List<Guid>();
        var dir = Path.Combine(storageRoot, "attachments", defectId.ToString("D"));
        Directory.CreateDirectory(dir);

        foreach (var f in files)
        {
            if (f.Length <= 0) continue;
            var ext = Path.GetExtension(f.FileName);
            if (!Allowed.Contains(ext)) throw new InvalidOperationException($"File type not allowed: {ext}");

            var id = Guid.NewGuid();
            var safeName = Regex.Replace(Path.GetFileName(f.FileName), @"[^\w\-.]+", "_");
            var key = $"{id:D}__{safeName}";
            var path = Path.Combine(dir, key);

            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                await f.CopyToAsync(stream);

            var a = new DefectAttachment
            {
                Id = id,
                DefectId = defectId,
                UploadedById = actorId,
                FileName = f.FileName,
                ContentType = f.ContentType ?? "application/octet-stream",
                Size = f.Length,
                StorageKey = key,
                UploadedAt = DateTime.UtcNow
            };
            _db.DefectAttachments.Add(a);
            await _db.SaveChangesAsync();

            _db.DefectHistories.Add(new DefectHistory {
                Id = Guid.NewGuid(), DefectId = defectId, ActorId = actorId,
                Type = "attachmentAdded", OccurredAt = DateTime.UtcNow,
                Payload = System.Text.Json.JsonSerializer.Serialize(new { a.Id, a.FileName })
            });
            await _db.SaveChangesAsync();

            saved.Add(id);
        }

        return saved.ToArray();
    }

    public async Task<(Stream stream, string contentType, string downloadName)> OpenAsync(Guid defectId, Guid fileId, Guid actorId, bool isManager)
    {
        var a = await _db.DefectAttachments.FirstOrDefaultAsync(x => x.Id == fileId && x.DefectId == defectId)
                ?? throw new KeyNotFoundException("File not found");

        var d = await _db.Defects.Select(x => new { x.Id, x.ProjectId }).FirstAsync(x => x.Id == defectId);
        var isMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == d.ProjectId && pm.UserId == actorId);
        if (!isMember) throw new UnauthorizedAccessException();

        var root = AppContext.GetData("StorageRoot") as string;
        if (string.IsNullOrEmpty(root)) throw new InvalidOperationException("Storage root not configured");

        var path = Path.Combine(root, "attachments", defectId.ToString("D"), a.StorageKey);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, a.ContentType, a.FileName);
    }

    public async Task DeleteAsync(Guid defectId, Guid fileId, Guid actorId, bool isManager, string storageRoot)
    {
        var a = await _db.DefectAttachments.FirstOrDefaultAsync(x => x.Id == fileId && x.DefectId == defectId)
                ?? throw new KeyNotFoundException("File not found");

        if (a.UploadedById != actorId && !isManager) throw new UnauthorizedAccessException();

        var path = Path.Combine(storageRoot, "attachments", defectId.ToString("D"), a.StorageKey);
        if (File.Exists(path)) File.Delete(path);

        _db.DefectAttachments.Remove(a);
        await _db.SaveChangesAsync();

        _db.DefectHistories.Add(new DefectHistory {
            Id = Guid.NewGuid(), DefectId = defectId, ActorId = actorId,
            Type = "attachmentDeleted", OccurredAt = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { fileId })
        });
        await _db.SaveChangesAsync();
    }
}
