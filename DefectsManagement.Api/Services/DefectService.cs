using DefectsManagement.Api.Data;
using DefectsManagement.Api.DTOs;
using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace DefectsManagement.Api.Services;

public class DefectService : IDefectService
{
    private readonly AppDbContext _db;
    private readonly IWorkflowService _workflow;

    public DefectService(AppDbContext db, IWorkflowService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    // === Create ===
    public async Task<DefectResponseDto> CreateAsync(CreateDefectDto dto, Guid actorId, bool isManager, bool isEngineer)
    {
        var project = await _db.Projects.FindAsync(dto.ProjectId)
                      ?? throw new ArgumentException("Project not found.");

        var priority = Enum.TryParse(dto.Priority, true, out DefectPriority p) ? p : DefectPriority.Medium;
        var defect = new Defect
        {
            ProjectId   = dto.ProjectId,
            Title       = dto.Title,
            Description = dto.Description,
            Priority    = priority,
            Status      = DefectStatus.New,
            CreatedById = actorId,
            AssignedId  = isManager ? dto.AssignedId : null,
            DueDate     = isManager ? dto.DueDate : null,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        if (dto.Tags is { Count: > 0 })
            defect.Tags = dto.Tags.Select(t => new DefectTag { DefectId = defect.Id, Tag = t }).ToList();

        _db.Defects.Add(defect);
        await _db.SaveChangesAsync();

        await _db.Entry(defect).Reference(d => d.AssignedTo).LoadAsync();

        return new DefectResponseDto
        {
            Id = defect.Id,
            Title = defect.Title,
            Description = defect.Description,
            Priority = defect.Priority.ToString(),
            Status = defect.Status.ToString(),
            CreatedAt = defect.CreatedAt,
            AssignedTo = defect.AssignedTo != null
                ? new AssignedToDto
                {
                    Id = defect.AssignedTo.Id,
                    Name = defect.AssignedTo.Name,
                    Username = defect.AssignedTo.Username
                }
                : null
        };
    }

    // === List ===
    public async Task<IEnumerable<DefectResponseDto>> ListAsync()
    {
        var list = await _db.Defects.Include(d => d.AssignedTo)
                                    .OrderByDescending(d => d.CreatedAt)
                                    .ToListAsync();

        return list.Select(d => new DefectResponseDto
        {
            Id = d.Id,
            Title = d.Title,
            Description = d.Description,
            Priority = d.Priority.ToString(),
            Status = d.Status.ToString(),
            CreatedAt = d.CreatedAt,
            AssignedTo = d.AssignedTo != null
                ? new AssignedToDto { Id = d.AssignedTo.Id, Name = d.AssignedTo.Name, Username = d.AssignedTo.Username }
                : null
        });
    }

    // === Get ===
    public async Task<DefectResponseDto?> GetAsync(Guid id)
    {
        var d = await _db.Defects.Include(x => x.AssignedTo).FirstOrDefaultAsync(x => x.Id == id);
        if (d == null) return null;

        return new DefectResponseDto
        {
            Id = d.Id,
            Title = d.Title,
            Description = d.Description,
            Priority = d.Priority.ToString(),
            Status = d.Status.ToString(),
            CreatedAt = d.CreatedAt,
            AssignedTo = d.AssignedTo != null
                ? new AssignedToDto { Id = d.AssignedTo.Id, Name = d.AssignedTo.Name, Username = d.AssignedTo.Username }
                : null
        };
    }

    // === Update ===
    public async Task UpdateAsync(Guid id, UpdateDefectDto dto, Guid actorId, bool isManager, bool isEngineer)
    {
        var d = await _db.Defects.Include(x => x.AssignedTo).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        if (isEngineer)
        {
            var isMine = d.CreatedById == actorId || d.AssignedId == actorId;
            if (!isMine) throw new SecurityException("Engineer can update only own/assigned defects.");
            if (d.Status is DefectStatus.Closed or DefectStatus.Canceled)
                throw new SecurityException("Engineer cannot edit final defects.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title)) d.Title = dto.Title!;
        if (!string.IsNullOrWhiteSpace(dto.Description)) d.Description = dto.Description!;

        if (dto.Priority != null)
        {
            if (!isManager) throw new SecurityException("Only Manager can change priority.");
            if (!Enum.TryParse(dto.Priority, true, out DefectPriority p))
                throw new ArgumentException("Invalid priority.");
            d.Priority = p;
        }

        if (dto.Status != null)
        {
            if (!Enum.TryParse(dto.Status, true, out DefectStatus s))
                throw new ArgumentException("Invalid status.");

            if (!_workflow.CanTransition(d.Status, s))
                throw new InvalidOperationException($"Forbidden transition {d.Status} → {s}");

            d.Status = s;
            if (s == DefectStatus.Closed) d.ClosedAt = DateTime.UtcNow;
        }

        if (dto.AssignedIdSet)
        {
            if (!isManager) throw new SecurityException("Only Manager can (re)assign.");
            d.AssignedId = dto.AssignedId;
        }

        if (dto.DueDateSet)
        {
            if (!isManager) throw new SecurityException("Only Manager can change due date.");
            d.DueDate = dto.DueDate;
        }

        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // === Assign (Manager) ===
    public async Task AssignAsync(Guid id, Guid? assignedId, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        d.AssignedId = assignedId;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // === Status change ===
    public async Task ChangeStatusAsync(Guid id, DefectStatus newStatus, Guid actorId, bool isManager, bool isEngineer)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        if (!_workflow.CanTransition(d.Status, newStatus) && !(isManager && newStatus == DefectStatus.InProgress && d.Status == DefectStatus.Closed))
            throw new InvalidOperationException($"Invalid transition {d.Status} → {newStatus}");

        if (isEngineer && !(d.CreatedById == actorId || d.AssignedId == actorId))
            throw new SecurityException("Engineer can change only own/assigned defects.");

        if (newStatus == DefectStatus.Canceled && !isManager)
            throw new SecurityException("Only Manager can cancel defects.");

        d.Status = newStatus;
        d.UpdatedAt = DateTime.UtcNow;
        if (newStatus == DefectStatus.Closed) d.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    // === Priority ===
    public async Task ChangePriorityAsync(Guid id, DefectPriority priority, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        d.Priority = priority;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // === DueDate ===
    public async Task ChangeDueDateAsync(Guid id, DateOnly? dueDate, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        d.DueDate = dueDate;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // === Tags ===
    public async Task PutTagsAsync(Guid id, IEnumerable<string> tags, Guid actorId)
    {
        var d = await _db.Defects.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        _db.DefectTags.RemoveRange(d.Tags);
        d.Tags = tags.Select(t => new DefectTag { DefectId = id, Tag = t }).ToList();
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // === Delete ===
    public async Task DeleteAsync(Guid id, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        if (d.Status is not (DefectStatus.New or DefectStatus.Canceled))
            throw new InvalidOperationException("Can delete only New or Canceled defects.");

        d.IsDeleted = true;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
