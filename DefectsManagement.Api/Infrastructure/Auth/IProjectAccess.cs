using System;
using System.Threading.Tasks;

namespace DefectsManagement.Api.Infrastructure.Auth;

public interface IProjectAccess
{
    Task<bool> IsMember(Guid projectId, Guid userId);
    Task<bool> IsManager(Guid projectId, Guid userId);
    Task<bool> IsEngineer(Guid projectId, Guid userId);
}
