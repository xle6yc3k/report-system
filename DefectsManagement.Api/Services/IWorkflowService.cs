using DefectsManagement.Api.Models;

namespace DefectsManagement.Api.Services;

public interface IWorkflowService
{
    bool CanTransition(DefectStatus from, DefectStatus to);
    DefectStatus[] Next(DefectStatus from);
}
