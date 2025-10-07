using DefectsManagement.Api.DTOs;

namespace DefectsManagement.Api.Services;

public interface ICommentService
{
    Task<Guid> AddAsync(Guid defectId, Guid authorId, string text);
    Task<IEnumerable<CommentResponseDto>> ListAsync(Guid defectId, DateTime? after, int limit);
    Task UpdateAsync(Guid defectId, Guid commentId, Guid actorId, string text, bool isManager);
    Task DeleteAsync(Guid defectId, Guid commentId, Guid actorId, bool isManager);
}
