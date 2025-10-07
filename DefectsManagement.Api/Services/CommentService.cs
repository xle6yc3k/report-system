using DefectsManagement.Api.Data;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    public CommentService(AppDbContext db) => _db = db;

    public async Task<Guid> AddAsync(Guid defectId, Guid authorId, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text required");

        var d = await _db.Defects.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == defectId)
                ?? throw new KeyNotFoundException("Defect not found");

        // любой участник проекта (включая Observer) может комментировать
        var isMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == d.ProjectId && pm.UserId == authorId);
        if (!isMember) throw new UnauthorizedAccessException();

        var c = new DefectComment { DefectId = defectId, AuthorId = authorId, Text = text, CreatedAt = DateTime.UtcNow };
        _db.DefectComments.Add(c);
        await _db.SaveChangesAsync();

        _db.DefectHistories.Add(new DefectHistory {
            Id = Guid.NewGuid(), DefectId = defectId, ActorId = authorId,
            Type = "commentAdded", OccurredAt = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { commentId = c.Id })
        });
        await _db.SaveChangesAsync();

        return c.Id;
    }

    public async Task<IEnumerable<CommentResponseDto>> ListAsync(Guid defectId, DateTime? after, int limit)
    {
        var q = _db.DefectComments
            .Where(c => c.DefectId == defectId);

        if (after.HasValue) q = q.Where(c => c.CreatedAt > after.Value);

        var items = await q
            .OrderBy(c => c.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(c => new CommentResponseDto {
                Id = c.Id,
                AuthorId = c.AuthorId,
                AuthorName = c.Author.Name,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                IsEdited = c.IsEdited,
                EditedAt = c.EditedAt
            })
            .ToListAsync();

        return items;
    }

    public async Task UpdateAsync(Guid defectId, Guid commentId, Guid actorId, string text, bool isManager)
    {
        var c = await _db.DefectComments.FirstOrDefaultAsync(x => x.Id == commentId && x.DefectId == defectId)
                ?? throw new KeyNotFoundException("Comment not found");

        if (c.AuthorId != actorId && !isManager) throw new UnauthorizedAccessException();

        c.Text = text;
        c.IsEdited = true;
        c.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _db.DefectHistories.Add(new DefectHistory {
            Id = Guid.NewGuid(), DefectId = defectId, ActorId = actorId,
            Type = "commentEdited", OccurredAt = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { commentId })
        });
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid defectId, Guid commentId, Guid actorId, bool isManager)
    {
        var c = await _db.DefectComments.FirstOrDefaultAsync(x => x.Id == commentId && x.DefectId == defectId)
                ?? throw new KeyNotFoundException("Comment not found");

        if (c.AuthorId != actorId && !isManager) throw new UnauthorizedAccessException();

        _db.DefectComments.Remove(c);
        await _db.SaveChangesAsync();

        _db.DefectHistories.Add(new DefectHistory {
            Id = Guid.NewGuid(), DefectId = defectId, ActorId = actorId,
            Type = "commentDeleted", OccurredAt = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { commentId })
        });
        await _db.SaveChangesAsync();
    }
}
