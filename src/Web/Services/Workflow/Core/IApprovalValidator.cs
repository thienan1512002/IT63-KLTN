using Web.Data.Entities.Workflow;

namespace Web.Services.Workflow.Core;

/// <summary>
/// Validates that the user action in <see cref="ApprovalActionContext"/> is permitted.
/// Throw <see cref="InvalidOperationException"/> when validation fails.
/// </summary>
public interface IApprovalValidator
{
    Task ValidateAsync(ApprovalActionContext context);
    Task UpdatePermissionValidateAsync(ApprovalRequest request, string userId);
}
