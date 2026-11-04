using Web.Common;

namespace Web.Data.Enums.Workflow;

public enum RequestStatus
{
    Unknown = 0,
    Ongoing = 1,
    Pending = Ongoing,
    Completed = 2,
    Returned = 3,
    Rejected = 4,
    Draft = 5,
    Cancelled = 6
}

public static class RequestStatusExtensions
{
    public static int GetProcessCode(this RequestStatus status, int approvedSteps) => status switch
    {
        RequestStatus.Ongoing => approvedSteps + 1,
        RequestStatus.Completed => approvedSteps,
        RequestStatus.Returned => WorkflowConstants.ApprovalStatusCode.Returned,
        RequestStatus.Rejected => WorkflowConstants.ApprovalStatusCode.Rejected,
        _ => approvedSteps
    };
}

public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum ApprovalStepStatus
{
    Waiting = 0,
    Pending = 1,
    Approved = 2,
    Returned = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum ApprovalAction
{
    Created = 0,
    Submitted = 1,
    Approved = 2,
    Returned = 3,
    Rejected = 4,
    Resubmitted = 5,
    Cancelled = 6,
    Updated = 7
}

public enum ApproverKind
{
    DepartmentManager = 0,
    DepartmentManagerByDepartment = 1,
    User = 2,
    Role = 3,
    SectionHead = 4
}
