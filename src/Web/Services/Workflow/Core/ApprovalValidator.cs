using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;

namespace Web.Services.Workflow.Core;

public class ApprovalValidator : IApprovalValidator
{
    private readonly IApproverResolver _approverResolver;

    public ApprovalValidator(IApproverResolver approverResolver)
    {
        _approverResolver = approverResolver;
    }

    public async Task ValidateAsync(ApprovalActionContext context)
    {
        if (string.IsNullOrEmpty(context.UserId))
            throw new InvalidOperationException("User ID is required");

        if (context.UserId == context.Request.FaxCode)
            throw new UnauthorizedAccessException("Không được tự phê duyệt yêu cầu của mình.");
        if (context.Request.Status != RequestStatus.Ongoing)
            throw new InvalidOperationException("Yêu cầu không ở trạng thái chờ phê duyệt.");
        if (context.ActionType is ApprovalActionType.Return or ApprovalActionType.Reject &&
            string.IsNullOrWhiteSpace(context.Comment))
            throw new InvalidOperationException("Bắt buộc nhập ý kiến khi trả lại hoặc từ chối.");

        if (context.Request.Complete || context.Request.Status == RequestStatus.Completed)
            throw new InvalidOperationException(
                $"{context.UserId} tried to act on request {context.Request.ApplicationNo}, but it has already been completed.");

        if (context.Request.Status == RequestStatus.Returned || context.Request.Status == RequestStatus.Rejected)
            throw new InvalidOperationException(
                $"{context.UserId} tried to act on request {context.Request.ApplicationNo}, but it has already been returned or rejected.");

        if (context.CurrentStep == null)
            throw new InvalidOperationException(
                $"Current approval step not found for request {context.Request.ApplicationNo}");

        if (!context.CurrentStep.IsCurrentStep)
            throw new InvalidOperationException(
                $"This step is not currently active for request {context.Request.ApplicationNo}");

        if (context.CurrentStep.IsApproved)
            throw new InvalidOperationException(
                $"{context.UserId} tried to act on request {context.Request.ApplicationNo}, but the current step has already been approved.");

        var isMainApprover = await _approverResolver.IsMainApproverAsync(
            context.CurrentStep.ApproverInfos,
            context.CurrentStep.ProcessOnlineOffline,
            context.CurrentStep.IsApproved,
            context.UserId);

        if (!isMainApprover)
            throw new InvalidOperationException(
                $"{context.UserId} is not the active approver for this step of request {context.Request.ApplicationNo}.");

        if (!context.CurrentStep.ActionTypes.HasFlag(context.ActionType))
            throw new InvalidOperationException(
                $"Action '{context.ActionType}' is not valid for the current step of request {context.Request.ApplicationNo}.");

        if (!context.CurrentStep.HasAction(context.ActionType))
            throw new InvalidOperationException(
                $"Action '{context.ActionType}' is not available for this step of request {context.Request.ApplicationNo}.");
    }

    public async Task UpdatePermissionValidateAsync(ApprovalRequest request, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new InvalidDataException("User ID is required");

        if (request == null)
            throw new InvalidDataException("Request not found to update");

        if (request.Status != RequestStatus.Returned)
            throw new InvalidOperationException(
                $"{userId} tried to update request {request.ApplicationNo}, but it is not in a returned state.");

        if (request.FaxCode != userId)
            throw new UnauthorizedAccessException(
                $"WARNING: User {userId} tried to update request {request.ApplicationNo}, but they are not the original submitter.");
    }

}
