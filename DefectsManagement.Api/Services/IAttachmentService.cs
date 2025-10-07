using DefectsManagement.Api.DTOs;
using Microsoft.AspNetCore.Http;

namespace DefectsManagement.Api.Services;

public interface IAttachmentService
{
    Task<IEnumerable<AttachmentResponseDto>> ListAsync(Guid defectId);
    Task<Guid[]> UploadAsync(Guid defectId, Guid actorId, IFormFileCollection files, string storageRoot);
    Task<(Stream stream, string contentType, string downloadName)> OpenAsync(Guid defectId, Guid fileId, Guid actorId, bool isManager);
    Task DeleteAsync(Guid defectId, Guid fileId, Guid actorId, bool isManager, string storageRoot);
}
