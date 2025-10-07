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
        // Базовые проверки
        var project = await _db.Projects.FindAsync(dto.ProjectId)
                      ?? throw new ArgumentException("Project not found.");

        if (!string.IsNullOrWhiteSpace(dto.Priority) &&
            !Enum.TryParse(dto.Priority, true, out DefectPriority _))
            throw new ArgumentException("Invalid priority.");

        // Если менеджер задал исполнителя — проверим, что такой пользователь существует
        if (isManager && dto.AssignedId is Guid assigneeOnCreate)
        {
            var assigneeExists = await _db.Users.AnyAsync(u => u.Id == assigneeOnCreate);
            if (!assigneeExists) throw new ArgumentException("Assigned user not found.");
        }

        var priority = Enum.TryParse(dto.Priority, true, out DefectPriority p)
            ? p
            : DefectPriority.Medium;

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
            defect.Tags = dto.Tags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(t => new DefectTag { DefectId = defect.Id, Tag = t })
                .ToList();

        _db.Defects.Add(defect);
        await _db.SaveChangesAsync();

        // история: создан
        await AddHistory(defect.Id, actorId, "created", new
        {
            title = defect.Title,
            priority = defect.Priority,
            status = defect.Status
        });

        // история: назначение/срок/теги если заданы
        if (defect.AssignedId.HasValue)
            await AddHistory(defect.Id, actorId, "assignedChanged", new { from = (Guid?)null, to = defect.AssignedId });

        if (defect.DueDate.HasValue)
            await AddHistory(defect.Id, actorId, "dueDateChanged", new { from = (DateOnly?)null, to = defect.DueDate });

        if (defect.Tags.Count > 0)
            await AddHistory(defect.Id, actorId, "tagsUpdated", new { tags = defect.Tags.Select(t => t.Tag).ToList() });

        // для ответа подтянем исполнителя (если есть)
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
        var list = await _db.Defects
            .Include(d => d.AssignedTo)
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
        var d = await _db.Defects
            .Include(x => x.AssignedTo)
            .FirstOrDefaultAsync(x => x.Id == id);

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
        var d = await _db.Defects
            .Include(x => x.AssignedTo)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Defect not found.");

        if (isEngineer)
        {
            var isMine = d.CreatedById == actorId || d.AssignedId == actorId;
            if (!isMine) throw new SecurityException("Engineer can update only own/assigned defects.");
            if (d.Status is DefectStatus.Closed or DefectStatus.Canceled)
                throw new SecurityException("Engineer cannot edit final defects.");
        }

        // Старые значения для истории
        var oldPriority = d.Priority;
        var oldStatus = d.Status;
        var oldAssignedId = d.AssignedId;
        var oldDueDate = d.DueDate;

        bool changedPriority = false, changedStatus = false, changedAssigned = false, changedDue = false;

        if (!string.IsNullOrWhiteSpace(dto.Title)) d.Title = dto.Title!;
        if (!string.IsNullOrWhiteSpace(dto.Description)) d.Description = dto.Description!;

        if (dto.Priority is not null)
        {
            if (!isManager) throw new SecurityException("Only Manager can change priority.");
            if (!Enum.TryParse(dto.Priority, true, out DefectPriority p))
                throw new ArgumentException("Invalid priority.");
            if (d.Priority != p) { d.Priority = p; changedPriority = true; }
        }

        if (dto.Status is not null)
        {
            if (!Enum.TryParse(dto.Status, true, out DefectStatus s))
                throw new ArgumentException("Invalid status.");

            if (!_workflow.CanTransition(d.Status, s))
                throw new InvalidOperationException($"Forbidden transition {d.Status} → {s}");

            if (d.Status != s)
            {
                d.Status = s;
                changedStatus = true;
                if (s == DefectStatus.Closed) d.ClosedAt = DateTime.UtcNow;
            }
        }

        if (dto.AssignedIdSet)
        {
            if (!isManager) throw new SecurityException("Only Manager can (re)assign.");
            if (dto.AssignedId.HasValue)
            {
                var exists = await _db.Users.AnyAsync(u => u.Id == dto.AssignedId.Value);
                if (!exists) throw new ArgumentException("Assigned user not found.");
            }
            if (d.AssignedId != dto.AssignedId)
            {
                d.AssignedId = dto.AssignedId;
                changedAssigned = true;
            }
        }

        if (dto.DueDateSet)
        {
            if (!isManager) throw new SecurityException("Only Manager can change due date.");
            if (d.DueDate != dto.DueDate)
            {
                d.DueDate = dto.DueDate;
                changedDue = true;
            }
        }

        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // История по изменённым полям
        if (changedPriority)
            await AddHistory(d.Id, actorId, "priorityChanged", new { from = oldPriority, to = d.Priority });

        if (changedStatus)
            await AddHistory(d.Id, actorId, "statusChanged", new { from = oldStatus, to = d.Status });

        if (changedAssigned)
            await AddHistory(d.Id, actorId, "assignedChanged", new { from = oldAssignedId, to = d.AssignedId });

        if (changedDue)
            await AddHistory(d.Id, actorId, "dueDateChanged", new { from = oldDueDate, to = d.DueDate });
    }

    // === Assign (Manager) ===
    public async Task AssignAsync(Guid id, Guid? assignedId, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        if (assignedId.HasValue)
        {
            var exists = await _db.Users.AnyAsync(u => u.Id == assignedId.Value);
            if (!exists) throw new ArgumentException("Assigned user not found.");
        }

        var oldAssignedId = d.AssignedId;

        d.AssignedId = assignedId;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddHistory(d.Id, actorId, "assignedChanged", new { from = oldAssignedId, to = assignedId });
    }

    // === Status change ===
    public async Task ChangeStatusAsync(Guid id, DefectStatus newStatus, Guid actorId, bool isManager, bool isEngineer)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        // Допустимость перехода: по графу, плюс спец-реопен менеджером Closed -> InProgress
        var allowed = _workflow.CanTransition(d.Status, newStatus)
                      || (isManager && newStatus == DefectStatus.InProgress && d.Status == DefectStatus.Closed);
        if (!allowed)
            throw new InvalidOperationException($"Invalid transition {d.Status} → {newStatus}");

        if (isEngineer && !(d.CreatedById == actorId || d.AssignedId == actorId))
            throw new SecurityException("Engineer can change only own/assigned defects.");

        if (newStatus == DefectStatus.Canceled && !isManager)
            throw new SecurityException("Only Manager can cancel defects.");

        var oldStatus = d.Status;

        d.Status = newStatus;
        d.UpdatedAt = DateTime.UtcNow;
        if (newStatus == DefectStatus.Closed) d.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await AddHistory(d.Id, actorId, "statusChanged", new { from = oldStatus, to = newStatus });
    }

    // === Priority ===
    public async Task ChangePriorityAsync(Guid id, DefectPriority priority, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        var oldPriority = d.Priority;

        d.Priority = priority;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddHistory(d.Id, actorId, "priorityChanged", new { from = oldPriority, to = priority });
    }

    // === DueDate ===
    public async Task ChangeDueDateAsync(Guid id, DateOnly? dueDate, Guid actorId)
    {
        var d = await _db.Defects.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        var oldDueDate = d.DueDate;

        d.DueDate = dueDate;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddHistory(d.Id, actorId, "dueDateChanged", new { from = oldDueDate, to = dueDate });
    }

    // === Tags ===
    public async Task PutTagsAsync(Guid id, IEnumerable<string> tags, Guid actorId)
    {
        var d = await _db.Defects.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Defect not found.");

        var oldTags = d.Tags.Select(t => t.Tag).OrderBy(t => t).ToList();

        _db.DefectTags.RemoveRange(d.Tags);

        var newTags = tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        d.Tags = newTags.Select(t => new DefectTag { DefectId = id, Tag = t }).ToList();
        d.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await AddHistory(d.Id, actorId, "tagsUpdated", new { from = oldTags, to = newTags });
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

        await AddHistory(d.Id, actorId, "deleted", new { });
    }

    // === History helper ===
    private async Task AddHistory(Guid defectId, Guid actorId, string type, object payload)
    {
        var h = new DefectHistory
        {
            Id = Guid.NewGuid(),
            DefectId = defectId,
            ActorId = actorId,
            Type = type,
            OccurredAt = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        };
        _db.DefectHistories.Add(h);
        await _db.SaveChangesAsync();
    }
}
