namespace Web.Services.Workflow.Core;

/// <summary>
/// Executes an approval action by mutating the in-memory state of an <see cref="ApprovalActionContext"/>.
/// No DB access — the caller is responsible for loading the context and saving changes afterwards.
/// </summary>
public interface IApprovalActionProcessor
{
    void Approve(ApprovalActionContext context);
    void Confirm(ApprovalActionContext context);
    void Reject(ApprovalActionContext context);
    void Return(ApprovalActionContext context);
}
