using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;

namespace DefectsManagement.Api.Infrastructure.Auth;

public class ProjectAccess : IProjectAccess
{
    private readonly AppDbContext _db;
    public ProjectAccess(AppDbContext db) => _db = db;

    public Task<bool> IsMember(Guid projectId, Guid userId) =>
        _db.ProjectMembers
           .AsNoTracking()
           .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

    public Task<bool> IsManager(Guid projectId, Guid userId) =>
        _db.ProjectMembers
           .AsNoTracking()
           .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.Role == ProjectRole.Manager);

    public Task<bool> IsEngineer(Guid projectId, Guid userId) =>
        _db.ProjectMembers
           .AsNoTracking()
           .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.Role == ProjectRole.Engineer);
}
