using Web.Data.Entities.Workflow;

namespace Web.Services.Workflow.Core;

/// <summary>
/// Builds and resets the <see cref="ApprovalDetail"/> records that represent
/// the workflow steps for a given <see cref="ApprovalRequest"/>.
/// </summary>
public interface IApprovalWorkflowBuilder
{
    /// <summary>
    /// Create and persist <see cref="ApprovalDetail"/> records for a new request,
    /// set the first step as current, and wire up Next/Return/Bypass FK links.
    /// </summary>
    Task InitiateAsync(ApprovalRequest request);

    /// <summary>
    /// Create approval details with manager-level step overrides
    /// without changing the stored approval template.
    /// </summary>
    Task InitManagerAwareWorkflowAsync(ApprovalRequest request);

    /// <summary>
    /// Remove all existing <see cref="ApprovalDetail"/> records for the request
    /// so a fresh set can be created on re-submission after edit.
    /// </summary>
    Task ResetDetailsAsync(ApprovalRequest request);

    /// <summary>
    /// Throw <see cref="InvalidDataException"/> if any step has no approvers assigned.
    /// Call this after <see cref="InitiateAsync"/> as a final guard.
    /// </summary>
    void EnsureApproversConfigured(List<ApprovalDetail> details);

    string GetEmailRequestName(string key);
}
