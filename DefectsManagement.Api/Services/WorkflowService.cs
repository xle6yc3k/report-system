using System;
using System.Collections.Generic;
using System.Linq;
using DefectsManagement.Api.Models;

namespace DefectsManagement.Api.Services;

public class WorkflowService : IWorkflowService
{
    // Граф переходов по твоей спецификации
    private static readonly IReadOnlyDictionary<DefectStatus, DefectStatus[]> _edges =
        new Dictionary<DefectStatus, DefectStatus[]>
        {
            [DefectStatus.New]        = new[] { DefectStatus.InProgress, DefectStatus.Canceled },
            [DefectStatus.InProgress] = new[] { DefectStatus.InReview,   DefectStatus.Canceled },
            [DefectStatus.InReview]   = new[] { DefectStatus.InProgress, DefectStatus.Closed, DefectStatus.Canceled },
            [DefectStatus.Closed]     = Array.Empty<DefectStatus>(),   // финальный (ре-открытие см. ниже)
            [DefectStatus.Canceled]   = Array.Empty<DefectStatus>()    // финальный
        };

    public bool CanTransition(DefectStatus from, DefectStatus to)
    {
        // спец-правило: ре-открытие Closed -> InProgress разрешено (дальше проверяешь роль в сервисе)
        if (from == DefectStatus.Closed && to == DefectStatus.InProgress)
            return true;

        return _edges.TryGetValue(from, out var next) && next.Contains(to);
    }

    public DefectStatus[] Next(DefectStatus from)
    {
        if (from == DefectStatus.Closed) return new[] { DefectStatus.InProgress };
        return _edges.TryGetValue(from, out var next) ? next : Array.Empty<DefectStatus>();
    }
}
