namespace DefectsManagement.Api.Models;

public enum DefectStatus
{
    New,
    InProgress,
    InReview,
    Closed,
    Canceled
}

public enum DefectPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ProjectRole
{
    Engineer = 0,
    Manager  = 1,
    Observer = 2
}
