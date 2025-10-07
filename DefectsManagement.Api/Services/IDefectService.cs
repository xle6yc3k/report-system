using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;

namespace DefectsManagement.Api.Services;

public interface IDefectService
{
    Task<DefectResponseDto> CreateAsync(CreateDefectDto dto, Guid actorId, bool isManager, bool isEngineer);
    Task<IEnumerable<DefectResponseDto>> ListAsync();
    Task<DefectResponseDto?> GetAsync(Guid id);
    Task UpdateAsync(Guid id, UpdateDefectDto dto, Guid actorId, bool isManager, bool isEngineer);
    Task AssignAsync(Guid id, Guid? assignedId, Guid actorId);
    Task ChangeStatusAsync(Guid id, DefectStatus newStatus, Guid actorId, bool isManager, bool isEngineer);
    Task ChangePriorityAsync(Guid id, DefectPriority priority, Guid actorId);
    Task ChangeDueDateAsync(Guid id, DateOnly? dueDate, Guid actorId);
    Task PutTagsAsync(Guid id, IEnumerable<string> tags, Guid actorId);
    Task DeleteAsync(Guid id, Guid actorId);
}
